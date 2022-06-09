<script setup>
import { ref, inject, onMounted } from "vue";
import { required } from "@vuelidate/validators";
import { useToast } from "vue-toastification";
import { useVuelidate } from "@vuelidate/core";
import arrIcons from "../../assets/json/icons.json";
import router from "@/router";
//init Model
const module = ref({
  module_name: "",
  STT: 1,
  Trangthai: true,
  IsAdmin: true,
  IsTarget: "_self",
});
//Valid Form
const submitted = ref(false);
const rules = {
  Name: {
    required,
    $errors: [
      {
        $property: "module_name",
        $validator: "required",
        $message: "Tên Menu không được để trống!",
      },
    ],
  },
};
const v$ = useVuelidate(rules, module);
//Khai báo biến
const store = inject("store");
const selectCapcha = ref();
const selectedKey = ref();
const selectedNodes = ref([]);
const filters = ref({});
const opition = ref({ search: "" });
const modules = ref();
const treemodules = ref();
const displayAddModule = ref(false);
const isFirst = ref(true);
let files = [];
const toast = useToast();
const swal = inject("$swal");
const axios = inject("axios"); // inject axios
const basedomainURL = baseURL;
const config = {
  headers: { Authorization: `Bearer ${store.getters.token}` },
};
const tdTargets = ref([
  { value: "_blank", text: "Mở sang tab mới" },
  { value: "_self", text: "Mở tab hiện tại" },
]);
const menuButs = ref();
const itemButs = ref([
  {
    label: "Xuất Excel",
    icon: "pi pi-file-excel",
    command: (event) => {
      exportModule("ExportExcel");
    },
  },
  {
    label: "Xuất Mẫu",
    icon: "pi pi-file-excel",
    command: (event) => {
      exportModule("ExportExcelMau");
    },
  },
]);

//Khai báo function
const toggleExport = (event) => {
  menuButs.value.toggle(event);
};
const onNodeSelect = (node) => {
  selectedNodes.value.push(node.data.module_id);
};
const onNodeUnselect = (node) => {
  selectedNodes.value.splice(selectedNodes.value.indexOf(node.data.module_id), 1);
};
const handleFileUpload = (event) => {
  files = event.target.files;
  var output = document.getElementById("moduleAnh");
  output.src = URL.createObjectURL(event.target.files[0]);
  output.onload = function () {
    URL.revokeObjectURL(output.src); // free memory
  };
};
//Show Modal
const showModalAddModule = () => {
  submitted.value = false;
  selectCapcha.value = {};
  module.value = {
    module_name: "",
    STT: modules.value.length + 1,
    Trangthai: true,
    IsAdmin: true,
    IsTarget: "_self",
  };
  displayAddModule.value = true;
};
const chonanh = (id) => {
  document.getElementById(id).click();
};
const closedisplayAddModule = () => {
  displayAddModule.value = false;
};
//Thêm sửa xoá
const onRefersh = () => {
  opition.value.search = "";
  loadModule(true);
};
const onSearch = () => {
  loadModule(true);
};
const renderTree = (data) => {
  let arrChils = [];
  let arrtreeChils = [];
  data
    .filter((x) => x.parent_id == null)
    .forEach((m, i) => {
      m.IsOrder = i + 1;
      let om = { key: m.module_id, data: m };
      const rechildren = (mm, module_id) => {
        let dts = data.filter((x) => x.parent_id == module_id);
        if (dts.length > 0) {
          if (!mm.children) mm.children = [];
          dts.forEach((em) => {
            let om1 = { key: em.module_id, data: em };
            rechildren(om1, em.module_id);
            mm.children.push(om1);
          });
        }
      };
      rechildren(om, m.module_id);
      arrChils.push(om);
      //
      om = { key: m.module_id, data: m.module_id, label: m.module_name };
      const retreechildren = (mm, module_id) => {
        let dts = data.filter((x) => x.parent_id == module_id);
        if (dts.length > 0) {
          if (!mm.children) mm.children = [];
          dts.forEach((em) => {
            let om1 = { key: em.module_id, data: em.module_id, label: em.module_name };
            retreechildren(om1, em.module_id);
            mm.children.push(om1);
          });
        }
      };
      retreechildren(om, m.module_id);
      arrtreeChils.push(om);
    });
  arrtreeChils.unshift({ key: -1, data: -1, label: "-----Chọn Module----" });
  modules.value = arrChils;
  treemodules.value = arrtreeChils;
  
};
const loadModule = (rf) => {
  if (rf) {
    opition.value.loading = true;
  }
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      { proc: "sys_modules_list", par: [{ par: "search", va: opition.value.search }] },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data)[0];
     
      if (data.length > 0) {
        renderTree(data);
      } else {
        modules.value = [];
      }
      if (isFirst.value) isFirst.value = false;
      if (rf) {
        opition.value.loading = false;
      }
    })
    .catch((error) => {
      console.log(error);
      
      if (error && error.status === 401) {
        swal.fire({
          title: "Error!",
          text: "Mã token đã hết hạn hoặc không hợp lệ, vui lòng đăng nhập lại!",
          icon: "error",
          confirmButtonText: "OK",
        });
      }
    });
};
const editModule = (md) => {
  submitted.value = false;
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  displayAddModule.value = true;
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      { proc: "Sys_Modules_Get", par: [{ par: "module_id", va: md.module_id }] },
      config
    )
    .then((response) => {
      swal.close();
      let data = JSON.parse(response.data.data);
      if (data.length > 0) {
        module.value = data[0][0];
        selectCapcha.value = {};
        selectCapcha.value[module.value.parent_id || "-1"] = true;
      }
    })
    .catch((error) => {
      if (error.status === 401) {
        swal.fire({
          title: "Error!",
          text: "Mã token đã hết hạn hoặc không hợp lệ, vui lòng đăng nhập lại!",
          icon: "error",
          confirmButtonText: "OK",
        });
      }
    });
};
const handleSubmit = (isFormValid) => {
  submitted.value = true;
  if (!isFormValid) {
    return;
  }
  let keys = Object.keys(selectCapcha.value);
  module.value.parent_id = keys[0];
  if (module.value.parent_id == -1) {
    module.value.parent_id = null;
  }
  addModule();
};
const addTreeModule = (md) => {
  selectCapcha.value = {};
  selectCapcha.value[md.module_id] = true;
  module.value = {
    module_name: "",
    STT: modules.value.length + 1,
    Trangthai: true,
    IsAdmin: true,
    IsTarget: "_self",
    parent_id: md.module_id,
  };
  submitted.value = false;
  displayAddModule.value = true;
};
const addModule = () => {
  let or = router.options.routes.find((x) => x.path == module.value.IsLink);
  if (or) {
    module.value.IsFilePath = or.component.toString().replace("() => import('", "").replace("')", "");
  }
  let formData = new FormData();
  for (var i = 0; i < files.length; i++) {
    let file = files[i];
    formData.append("anh", file);
  }
  formData.append("model", JSON.stringify(module.value));
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  axios({
    method: module.value.module_id ? "put" : "post",
    url:
      baseURL + `/api/Modules/${module.value.module_id ? "Update_Module" : "Add_Module"}`,
    data: formData,
    headers: {
      Authorization: `Bearer ${store.getters.token}`,
    },
  })
    .then((response) => {
      if (response.data.err != "1") {
        swal.close();
        toast.success("Cập nhật Module thành công!");
        loadModule();
        closedisplayAddModule();
      } else {
        swal.fire({
          title: "Error!",
          text: response.data.ms,
          icon: "error",
          confirmButtonText: "OK",
        });
      }
    })
    .catch((error) => {
      swal.close();
      swal.fire({
        title: "Error!",
        text: "Có lỗi xảy ra, vui lòng kiểm tra lại!",
        icon: "error",
        confirmButtonText: "OK",
      });
    });
};

const delModule = (md) => {
  swal
    .fire({
      title: "Thông báo",
      text: "Bạn có muốn xoá menu này không!",
      icon: "warning",
      showCancelButton: true,
      confirmButtonColor: "#3085d6",
      cancelButtonColor: "#d33",
      confirmButtonText: "Có",
      cancelButtonText: "Không",
    })
    .then((result) => {
      if (result.isConfirmed) {
        swal.fire({
          width: 110,
          didOpen: () => {
            swal.showLoading();
          },
        });
        axios
          .delete(baseURL + "/api/Modules/Del_Module", {
            headers: { Authorization: `Bearer ${store.getters.token}` },
            data: md != null ? [md.module_id] : selectedNodes.value,
          })
          .then((response) => {
            swal.close();
            if (response.data.err != "1") {
              swal.close();
              toast.success("Xoá Module thành công!");
              loadModule();
              if (!md) selectedNodes.value = [];
            } else {
              swal.fire({
                title: "Error!",
                text: response.data.ms,
                icon: "error",
                confirmButtonText: "OK",
              });
            }
          })
          .catch((error) => {
            swal.close();
            if (error.status === 401) {
              swal.fire({
                title: "Error!",
                text: "Mã token đã hết hạn hoặc không hợp lệ, vui lòng đăng nhập lại!",
                icon: "error",
                confirmButtonText: "OK",
              });
            }
          });
      }
    });
};

const exportModule = (method) => {
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  axios
    .get(baseURL + `/api/Modules/${method}`, {
      headers: { Authorization: `Bearer ${store.getters.token}` },
    })
    .then((response) => {
      swal.close();
      if (response.data.err != "1") {
        swal.close();
        toast.success("Kết xuất Module thành công!");
        window.open(baseURL + response.data.path);
      } else {
        swal.fire({
          title: "Error!",
          text: response.data.ms,
          icon: "error",
          confirmButtonText: "OK",
        });
      }
    })
    .catch((error) => {
      if (error.status === 401) {
        swal.fire({
          title: "Error!",
          text: "Mã token đã hết hạn hoặc không hợp lệ, vui lòng đăng nhập lại!",
          icon: "error",
          confirmButtonText: "OK",
        });
      }
    });
};
const filteredItems = ref([]);
const searchItems = (event) => {
  //in a real application, make a request to a remote url with the query and return filtered results, for demo we filter at client side
  let query = event.query;
  let filteItems = [];
  for (let i = 0; i < arrIcons.length; i++) {
    let item = arrIcons[i];
    if (item.toLowerCase().indexOf(query.toLowerCase()) != -1) {
      filteItems.push(item);
    }
  }
  filteredItems.value = filteItems;
};
onMounted(() => {
  //init
  loadModule(true);
  return {
    displayAddModule,
    isFirst,
    opition,
    showModalAddModule,
    closedisplayAddModule,
    addModule,
    addTreeModule,
    editModule,
    onSearch,
    delModule,
    handleFileUpload,
    chonanh,
    v$,
    handleSubmit,
    submitted,
    basedomainURL,
    filters,
    onRefersh,
    treemodules,
    itemButs,
    menuButs,
    toggleExport,
    selectedKey,
    onNodeSelect,
    onNodeUnselect,
    selectedNodes,
    selectCapcha,
  };
});
</script>
<template>
  <div class="main-layout flex-grow-1 p-2" v-if="store.getters.islogin">
    <TreeTable
      :value="modules"
      v-model:selectionKeys="selectedKey"
      :loading="opition.loading"
      @nodeSelect="onNodeSelect"
      @nodeUnselect="onNodeUnselect"
      :filters="filters"
      :showGridlines="true"
      selectionMode="checkbox"
      filterMode="lenient"
      class="p-treetable-sm e-sm"
      :paginator="modules && modules.length > 20"
      :rows="20"
      :scrollable="true"
      scrollHeight="flex"
    >
      <template #header>
        <h3 class="module-title mt-0 ml-1 mb-2">
          <i class="pi pi-microsoft"></i> Module chức năng
        </h3>
        <Toolbar class="w-full custoolbar">
          <template #start>
            <span class="p-input-icon-left">
              <i class="pi pi-search" />
              <InputText
                type="text"
                spellcheck="false"
                v-model="opition.search"
                placeholder="Tìm kiếm"
                v-on:keyup.enter="onSearch"
              />
            </span>
          </template>

          <template #end>
            <Button
              class="mr-2 p-button-outlined p-button-secondary"
              icon="pi pi-refresh"
              @click="onRefersh"
            />
            <Button
              label="Xoá"
              icon="pi pi-trash"
              class="mr-2 p-button-danger"
              v-if="selectedNodes.length > 0"
              @click="delModule"
            />
            <Button
              label="Export"
              icon="pi pi-file-excel"
              class="mr-2 p-button-outlined p-button-secondary"
              @click="toggleExport"
              aria-haspopup="true"
              aria-controls="overlay_Export"
            />
            <Menu id="overlay_Export" ref="menuButs" :model="itemButs" :popup="true" />
            <Button
              label="Thêm Module"
              icon="pi pi-plus"
              class="mr-2"
              @click="showModalAddModule"
            />
          </template>
        </Toolbar>
      </template>
      <Column
        :sortable="true"
        field="IsOrder"
        header="No."
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:120px"
        bodyStyle="text-align:center;max-width:120px"
      ></Column>
      <Column
        field="module_id"
        :sortable="true"
        header="Mã Menu"
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:120px"
        bodyStyle="text-align:center;max-width:120px"
      ></Column>
      <Column
        field="Icon"
        header="Icon Menu"
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:100px"
        bodyStyle="text-align:center;max-width:100px"
      >
        <template #body="md">
          <i v-bind:class="md.node.data.Icon"></i>
        </template>
      </Column>
      <Column field="module_name" header="Tên Menu" :sortable="true" :expander="true"> </Column>
      <Column
        field="IsLink"
        header="Link"
        class="align-items-center justify-content-center"
        headerStyle="text-align:center;max-width:250px"
        bodyStyle="text-align:center;max-width:250px"
      >
        ></Column
      >
      <Column
        field="STT"
        :sortable="true"
        header="STT"
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:100px"
        bodyStyle="text-align:center;max-width:100px"
      ></Column>
      <Column
        headerClass="text-center"
        headerStyle="text-align:center;max-width:150px"
        bodyStyle="text-align:center;max-width:150px"
      >
        <template #header> </template>
        <template #body="md">
          <Button
            type="button"
            icon="pi pi-plus-circle"
            class="p-button-rounded p-button-sm p-button-success"
            style="margin-right: 0.5rem"
            @click="addTreeModule(md.node.data)"
          ></Button>
          <Button
            type="button"
            icon="pi pi-pencil"
            class="p-button-rounded p-button-sm p-button-info"
            style="margin-right: 0.5rem"
            @click="editModule(md.node.data)"
          ></Button>
          <Button
            type="button"
            icon="pi pi-trash"
            class="p-button-rounded p-button-sm p-button-danger"
            @click="delModule(md.node.data)"
          ></Button>
        </template>
      </Column>
      <template #empty>
        <div
          class="align-items-center justify-content-center p-4 text-center"
          v-if="!isFirst"
        >
          <img src="../../assets/background/nodata.png" height="144" />
          <h3 class="m-1">Không có dữ liệu</h3>
        </div>
      </template>
    </TreeTable>
  </div>
  <Dialog
    header="Cập nhật Module"
    v-model:visible="displayAddModule"
    :style="{ width: '40vw', zIndex: 1000 }"
    :maximizable="true"
    :autoZIndex="false"
    :modal="true"
  >
    <form @submit.prevent="handleSubmit(!v$.$invalid)">
      <div class="grid formgrid m-2">
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left">Tên menu <span class="redsao">(*)</span></label>
          <InputText
            spellcheck="false"
            class="col-10 ip36"
            v-model="module.module_name"
            :class="{ 'p-invalid': v$.module_name.$invalid && submitted }"
          />
        </div>
        <small
          v-if="
            (v$.module_name.$invalid && submitted) || v$.module_name.$pending.$response
          "
          class="col-10 p-error"
        >
          <div class="field col-12 md:col-12">
            <label class="col-2 text-left"></label>
            <span class="col-10 pl-3">{{
              v$.module_name.required.$message
                .replace("Value", "Tên menu")
                .replace("is required", "không được để trống")
            }}</span>
          </div></small
        >
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left">Cấp cha</label>
          <TreeSelect
            class="col-10"
            v-model="selectCapcha"
            :options="treemodules"
            :showClear="true"
            placeholder=""
            optionLabel="data.module_name"
            optionValue="data.module_id"
          ></TreeSelect>
        </div>

        <div class="col-8">
          <div class="field">
            <label class="col-3 text-left">Icon</label>
            <AutoComplete
              class="col-8 ip36 p-0"
              v-model="module.Icon"
              :suggestions="filteredItems"
              @complete="searchItems"
              :virtualScrollerOptions="{ itemSize: 20 }"
              dropdown
            >
              <template #item="slotProps">
                <div class="flex align-items-center">
                  <i :class="slotProps.item" style="font-size: large"></i>
                  <div class="ml-2">{{ slotProps.item }}</div>
                </div>
              </template>
            </AutoComplete>
          </div>
          <div class="field">
            <label class="col-3 text-left">Link</label>
            <Dropdown
              class="col-8 ip36 p-0"
              v-model="module.IsLink"
              :options="router.options.routes"
              optionLabel="path"
              optionValue="path"
              :editable="true"
            />
          </div>
          <div class="field">
            <label class="col-3 text-left">Kiểu mở</label>
            <Dropdown
              class="col-8"
              v-model="module.IsTarget"
              :options="tdTargets"
              optionLabel="text"
              optionValue="value"
              placeholder="Chọn kiểu mở"
            />
          </div>
        </div>
        <div class="col-4">
          <div class="field">
            <label class="col-12 text-rigth">Ảnh</label>
            <div class="inputanh" @click="chonanh('AnhModule')">
              <img
                id="moduleAnh"
                v-bind:src="
                  module.Anh ? basedomainURL + module.Anh : '/src/assets/image/noimg.jpg'
                "
              />
            </div>
            <input
              class="ipnone"
              id="AnhModule"
              type="file"
              accept="image/*"
              @change="handleFileUpload"
            />
          </div>
        </div>
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left">STT</label>
          <InputNumber class="col-2 ip36 p-0" v-model="module.STT" />
          <label style="vertical-align: text-bottom" class="col-4 text-right"
            >Trạng thái</label
          >
          <InputSwitch v-model="module.Trangthai" class="mt-1" />
          <label style="vertical-align: text-bottom" class="col-2 text-right"
            >Admin</label
          >
          <InputSwitch v-model="module.IsAdmin" />
        </div>
      </div>
    </form>
    <template #footer>
      <Button
        label="Huỷ"
        icon="pi pi-times"
        @click="closedisplayAddModule"
        class="p-button-raised p-button-secondary"
      />
      <Button
        label="Cập nhật"
        icon="pi pi-save"
        @click="handleSubmit(!v$.$invalid)"
        
      />
    </template>
  </Dialog>
</template>
<style scoped>
.ipnone {
  display: none;
}
.inputanh {
  border: 1px solid #ccc;
  width: 96px;
  height: 96px;
  cursor: pointer;
  padding: 1px;
}
.inputanh img {
  object-fit: cover;
  width: 100%;
  height: 100%;
}
</style>
