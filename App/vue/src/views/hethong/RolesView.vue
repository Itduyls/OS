<script setup>
import { ref, inject, onMounted } from "vue";
import { required } from "@vuelidate/validators";
import { useToast } from "vue-toastification";
import { useVuelidate } from "@vuelidate/core";
//init Model
const role = ref({
  Role_Name: "",
  STT: 1,
  Trangthai: true,
});
//Valid Form
const submitted = ref(false);
const rules = {
  Role_ID: {
    required,
    $errors: [
      {
        $property: "Role_ID",
        $validator: "required",
        $message: "Mã Nhóm không được để trống!",
      },
    ],
  },
  Role_Name: {
    required,
    $errors: [
      {
        $property: "Role_Name",
        $validator: "required",
        $message: "Tên Nhóm không được để trống!",
      },
    ],
  },
};
const v$ = useVuelidate(rules, role);
//Khai báo biến
const isAdd = ref(true);
const selectedNodes = ref([]);
const filters = ref({});
const opition = ref({ search: "" });
const roles = ref();
const treeroles = ref();
const displayAddRole = ref(false);
const isFirst = ref(true);
let files = [];
const store = inject("store");
const toast = useToast();
const swal = inject("$swal");
const axios = inject("axios"); // inject axios
const basedomainURL = baseURL;
const config = {
  headers: { Authorization: `Bearer ${store.getters.token}` },
};
const tdQuyens = [
  { value: 0, text: "Không có quyền (0)" },
  { value: 1, text: "Xem cá nhân (1)" },
  { value: 2, text: "Xem tất cả (2)" },
  { value: 3, text: "Chỉnh sửa cá nhân (3)" },
  { value: 4, text: "Chỉnh sửa tất cả (4)" },
  { value: 5, text: "Duyệt (5)" },
  { value: 6, text: "Full (6)" },
].reverse();
const quyen = ref({});
const menuQuyen = ref();
const itemQuyens = ref([]);
tdQuyens.forEach((element) => {
  itemQuyens.value.push({
    label: element.text,
    icon: "pi pi-key",
    command: (event) => {
      quyen.value.IsQuyen = element.value;
    },
  });
});
const togglQuyen = (event, q) => {
  quyen.value = q;
  menuQuyen.value.toggle(event);
};
const menuButs = ref();
const itemButs = ref([
  {
    label: "Xuất Excel",
    icon: "pi pi-file-excel",
    command: (event) => {
      exportRole("ExportExcel");
    },
  },
  {
    label: "Xuất Mẫu",
    icon: "pi pi-file-excel",
    command: (event) => {
      exportRole("ExportExcelMau");
    },
  },
]);

//Khai báo function
const toggleExport = (event) => {
  menuButs.value.toggle(event);
};
const handleFileUpload = (event) => {
  files = event.target.files;
};
//Show Modal
const showModalAddRole = () => {
  isAdd.value = true;
  submitted.value = false;
  role.value = {
    Role_Name: "",
    STT: roles.value.length + 1,
    Trangthai: true,
  };
  displayAddRole.value = true;
};
const chonanh = (id) => {
  document.getElementById(id).click();
};
const closedisplayAddRole = () => {
  displayAddRole.value = false;
};
//Thêm sửa xoá
const onRefersh = () => {
  opition.value.search = "";
  loadRole(true);
};
const onSearch = () => {
  loadRole(true);
};
const loadRole = (rf) => {
  if (rf) {
    opition.value.loading = true;
  }
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      { proc: "Sys_Roles_List", par: [{ par: "s", va: opition.value.search }] },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data)[0];
      if (data.length > 0) {
        roles.value = data;
      } else {
        roles.value = [];
      }
      if (isFirst.value) isFirst.value = false;
      if (rf) {
        opition.value.loading = false;
      }
    })
    .catch((error) => {
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
const displayConfigRole = ref(false);
const modules = ref([]);
const renderTree = (data) => {
  let arrChils = [];
  data
    .filter((x) => x.Capcha_ID == null)
    .forEach((m, i) => {
      m.IsOrder = i + 1;
      let om = { key: m.Module_ID, data: m };
      const rechildren = (mm, Module_ID) => {
        let dts = data.filter((x) => x.Capcha_ID == Module_ID);
        if (dts.length > 0) {
          if (!mm.children) mm.children = [];
          dts.forEach((em) => {
            let om1 = { key: em.Module_ID, data: em };
            rechildren(om1, em.Module_ID);
            mm.children.push(om1);
          });
        }
      };
      rechildren(om, m.Module_ID);
      arrChils.push(om);
    });
  modules.value = arrChils;
};
const configRole = (md) => {
  //Config quyền
  displayConfigRole.value = true;
  opition.value.moduleloading = true;
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      { proc: "ModuleListByRole", par: [{ par: "Role_ID ", va: md.Role_ID }] },
      config
    )
    .then((response) => {
      opition.value.moduleloading = false;
      let data = JSON.parse(response.data.data)[0];
      if (data.length > 0) {
        data
          .filter((x) => x.IsQuyen)
          .forEach((r) => {
            let ds = r.IsQuyen.toString().split("");
            var arrs = [];
            ds.forEach((e) => {
              arrs.push(parseInt(e));
            });
            r.IsQuyen = arrs;
          });
        renderTree(data);
      } else {
        modules.value = [];
      }
    })
    .catch((error) => {
      opition.value.moduleloading = false;
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
const closedisplayConfigRole = () => {
  displayConfigRole.value = false;
};
const changeQuyen = (md) => {
  if (md.children) {
    md.children.forEach((element) => {
      element.data.IsQuyen = md.data.IsQuyen;
    });
  }
};
const addConfigRole = () => {
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  let mdmodules = [];
  modules.value.forEach((element) => {
    mdmodules.push({
      RoleModule_ID: element.data.RoleModule_ID || -1,
      Role_ID: element.data.Role_ID,
      Module_ID: element.data.Module_ID,
      IsCap: element.data.IsCap,
      IsQuyen: element.data.IsQuyen
        ? element.data.IsQuyen.join("")
        : element.data.IsQuyen,
    });
    if (element.children && element.children.length > 0) {
      element.children.forEach((ec) => {
        mdmodules.push({
          RoleModule_ID: ec.data.RoleModule_ID || -1,
          Role_ID: ec.data.Role_ID,
          Module_ID: ec.data.Module_ID,
          IsCap: ec.data.IsCap,
          IsQuyen: ec.data.IsQuyen ? ec.data.IsQuyen.join("") : ec.data.IsQuyen,
        });
      });
    }
  });
  axios({
    method: "post",
    url: baseURL + "/api/Roles/Add_RoleModules",
    data: mdmodules,
    headers: {
      Authorization: `Bearer ${store.getters.token}`,
    },
  })
    .then((response) => {
      swal.close();
      if (response.data.err != "1") {
        swal.close();
        toast.success("Phân quyền Role thành công!");
        displayConfigRole.value = false;
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
};
const editRole = (md) => {
  isAdd.value = false;
  submitted.value = false;
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  displayAddRole.value = true;
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      { proc: "Sys_Roles_Get", par: [{ par: "Role_ID", va: md.Role_ID }] },
      config
    )
    .then((response) => {
      swal.close();
      let data = JSON.parse(response.data.data);
      if (data.length > 0) {
        role.value = data[0][0];
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
  addRole();
};
const addRole = () => {
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  axios({
    method: isAdd.value == false ? "put" : "post",
    url: baseURL + `/api/Roles/${isAdd.value == false ? "Update_Roles" : "Add_Roles"}`,
    data: role.value,
    headers: {
      Authorization: `Bearer ${store.getters.token}`,
    },
  })
    .then((response) => {
      if (response.data.err != "1") {
        swal.close();
        toast.success("Cập nhật Role thành công!");
        loadRole();
        closedisplayAddRole();
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

const delRole = (md) => {
  swal
    .fire({
      title: "Thông báo",
      text: "Bạn có muốn xoá nhóm người dùng này không!",
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
          .delete(baseURL + "/api/Roles/Del_Roles", {
            headers: { Authorization: `Bearer ${store.getters.token}` },
            data: md != null ? [md.Role_ID] : selectedNodes.value.map((x) => x.Role_ID),
          })
          .then((response) => {
            swal.close();
            if (response.data.err != "1") {
              swal.close();
              toast.success("Xoá Role thành công!");
              loadRole();
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

const upTrangthaiRole = (md) => {
  let ids = [md.Role_ID];
  let tts = [md.Trangthai];
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  axios({
    method: "put",
    url: baseURL + "/api/Roles/Update_TrangthaiRoles",
    data: { ids: ids, tts: tts },
    headers: {
      Authorization: `Bearer ${store.getters.token}`,
    },
  })
    .then((response) => {
      swal.close();
      if (response.data.err != "1") {
        swal.close();
        toast.success("Cập nhật trạng thái Role thành công!");
        loadRole();
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
};

const exportRole = (method) => {
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  let par = [{ par: "name", va: "Sys_Modules" }];
  if (method != "ExportExcelMau") {
    par = [{ par: "Users_ID", va: opition.value.Users_ID }];
  }
  axios
    .post(
      baseURL + "/api/Excel/" + method,
      {
        excelname: "DANH SÁCH NHÓM NGƯỜI DÙNG",
        proc: "Sys_Roles_ListExport",
        par: par,
      },
      config
    )
    .then((response) => {
      swal.close();
      if (response.data.err != "1") {
        swal.close();
        toast.success("Kết xuất Data thành công!");
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

onMounted(() => {
  //init
  loadRole(true);
  return {
    displayAddRole,
    isFirst,
    opition,
    showModalAddRole,
    closedisplayAddRole,
    addRole,
    editRole,
    onSearch,
    delRole,
    handleFileUpload,
    chonanh,
    v$,
    handleSubmit,
    submitted,
    basedomainURL,
    filters,
    onRefersh,
    treeroles,
    itemButs,
    menuButs,
    toggleExport,
    selectedNodes,
    upTrangthaiRole,
  };
});
</script>
<template>
  <div class="main-layout flex-grow-1 p-2" v-if="store.getters.islogin">
    <DataTable
      class="w-full p-datatable-sm e-sm"
      :value="roles"
      :paginator="roles && roles.length > 20"
      :loading="opition.loading"
      :rows="20"
      dataKey="Role_ID"
      :showGridlines="true"
      :rowHover="true"
      v-model:selection="selectedNodes"
      :filters="filters"
      filterMode="lenient"
      paginatorTemplate="FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink CurrentPageReport RowsPerPageDropdown"
      :rowsPerPageOptions="[10, 25, 50]"
      currentPageReportTemplate="Showing {first} to {last} of {totalRecords} entries"
      responsiveLayout="scroll"
      :scrollable="true"
      scrollHeight="flex"
    >
      <template #header>
        <h3 class="module-title mt-0 ml-1 mb-2">
          <i class="pi pi-list"></i> Nhóm người dùng
          <span v-if="roles">({{ roles.length }})</span>
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
              @click="delRole"
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
              label="Thêm Role"
              icon="pi pi-plus"
              class="mr-2"
              @click="showModalAddRole"
            />
          </template>
        </Toolbar>
      </template>
      <Column
        selectionMode="multiple"
        headerStyle="text-align:center;max-width:50px"
        bodyStyle="text-align:center;max-width:50px"
        class="align-items-center justify-content-center text-center"
      ></Column>
      <Column
        :sortable="true"
        field="STT"
        header="STT"
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:100px"
        bodyStyle="text-align:center;max-width:100px"
      ></Column>
      <Column
        field="Role_ID"
        :sortable="true"
        header="Mã Nhóm"
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:150px"
        bodyStyle="text-align:center;max-width:150px"
      >
        <template #body="md">
          <span v-bind:class="'Trangthai' + md.data.Trangthai">{{
            md.data.Role_ID
          }}</span>
        </template>
      </Column>
      <Column field="Role_Name" header="Tên Nhóm" :sortable="true">
        <template #body="md">
          <Chip
            :style="{ background: md.data.Maunen, color: md.data.Mauchu }"
            v-bind:label="md.data.Role_Name"
            class="mr-2 mb-2"
          />
        </template>
      </Column>
      <Column
        field="Trangthai"
        header="Trạng thái"
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:120px"
        bodyStyle="text-align:center;max-width:120px"
      >
        <template #body="md">
          <Checkbox
            v-model="md.data.Trangthai"
            :binary="true"
            @change="upTrangthaiRole(md.data)"
          />
        </template>
      </Column>
      <Column
        headerClass="text-center"
        bodyClass="text-center"
        headerStyle="text-align:center;max-width:150px"
        bodyStyle="text-align:center;max-width:150px"
      >
        <template #header> </template>
        <template #body="md">
          <Button
            type="button"
            icon="pi pi-key"
            class="p-button-rounded p-button-sm p-button-success"
            style="margin-right: 0.5rem"
            @click="configRole(md.data)"
          ></Button>
          <Button
            type="button"
            icon="pi pi-pencil"
            class="p-button-rounded p-button-sm p-button-info"
            style="margin-right: 0.5rem"
            @click="editRole(md.data)"
          ></Button>
          <Button
            type="button"
            icon="pi pi-trash"
            class="p-button-rounded p-button-sm p-button-danger"
            @click="delRole(md.data)"
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
    </DataTable>
  </div>
  <Dialog
    header="Cập nhật nhóm người dùng"
    v-model:visible="displayAddRole"
    :style="{ width: '480px', zIndex: 1000 }"
    :maximizable="true"
    :autoZIndex="false"
    :modal="true"
  >
    <form @submit.prevent="handleSubmit(!v$.$invalid)">
      <div class="grid formgrid m-2">
        <div class="field col-12 md:col-12">
          <label class="col-4 text-left">Mã nhóm <span class="redsao">(*)</span></label>
          <InputText
            spellcheck="false"
            v-bind:disabled="!isAdd"
            class="col-8 ip36"
            v-model="role.Role_ID"
            :class="{ 'p-invalid': v$.Role_ID.$invalid && submitted }"
          />
        </div>
        <small
          v-if="(v$.Role_ID.$invalid && submitted) || v$.Role_ID.$pending.$response"
          class="col-10 p-error"
        >
          <div class="field col-12 md:col-12">
            <label class="col-4 text-left"></label>
            <span class="col-8 pl-3">{{
              v$.Role_ID.required.$message
                .replace("Value", "Mã nhóm")
                .replace("is required", "không được để trống")
            }}</span>
          </div></small
        >
        <div class="field col-12 md:col-12">
          <label class="col-4 text-left">Tên nhóm <span class="redsao">(*)</span></label>
          <InputText
            spellcheck="false"
            class="col-8 ip36"
            v-model="role.Role_Name"
            :class="{ 'p-invalid': v$.Role_Name.$invalid && submitted }"
          />
        </div>
        <small
          v-if="(v$.Role_Name.$invalid && submitted) || v$.Role_Name.$pending.$response"
          class="col-10 p-error"
        >
          <div class="field col-12 md:col-12">
            <label class="col-4 text-left"></label>
            <span class="col-8 pl-3">{{
              v$.Role_Name.required.$message
                .replace("Value", "Tên nhóm")
                .replace("is required", "không được để trống")
            }}</span>
          </div></small
        >
        <div class="field col-12 md:col-12">
          <label class="col-4 text-left">Màu chữ</label>
          <InputText
            type="color"
            spellcheck="false"
            class="col-2"
            v-model="role.Mauchu"
          />
          <label class="col-4 text-right">Màu nền</label>
          <InputText
            type="color"
            spellcheck="false"
            class="col-2"
            v-model="role.Maunen"
          />
        </div>
        <div class="field col-12 md:col-12">
          <label class="col-4 text-left">STT</label>
          <InputNumber class="col-2 ip36 p-0" v-model="role.STT" />
        </div>
        <div class="field col-12 md:col-12">
          <label style="vertical-align: text-bottom" class="col-4 text-left"
            >Trạng thái</label
          >
          <InputSwitch v-model="role.Trangthai" class="mt-1" />
        </div>
      </div>
    </form>
    <template #footer>
      <Button
        label="Huỷ"
        icon="pi pi-times"
        @click="closedisplayAddRole"
        class="p-button-raised p-button-secondary"
      />
      <Button label="Cập nhật" icon="pi pi-save" @click="handleSubmit(!v$.$invalid)" />
    </template>
  </Dialog>
  <Dialog
    header="Phân quyền cho nhóm người dùng"
    v-model:visible="displayConfigRole"
    :style="{ width: '850px', zIndex: 1000 }"
    :maximizable="true"
    :autoZIndex="false"
    :modal="true"
  >
    <TreeTable
      :value="modules"
      :loading="opition.moduleloading"
      :showGridlines="true"
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
      </template>
      <Column
        field="Icon"
        :expander="true"
        header=""
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:100px"
        bodyStyle="text-align:center;max-width:100px"
      >
        <template #body="md">
          <i v-bind:class="md.node.data.Icon"></i>
        </template>
      </Column>
      <Column field="Module_Name" header="Tên Menu"> </Column>
      <Column
        field="IsQuyen"
        header="Mã Quyền"
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:150px"
        bodyStyle="max-width:150px"
      >
        <template #body="md">
          <b v-if="md.node.data.IsQuyen">{{ md.node.data.IsQuyen.join("") }}</b>
        </template>
      </Column>
      <Column
        headerClass="align-items-center justify-content-center text-center"
        header="Quyền"
        headerStyle="text-align:center;width:250px"
        bodyStyle="text-align:center;width:250px"
      >
        <template #body="md">
          <MultiSelect
            v-model="md.node.data.IsQuyen"
            @change="changeQuyen(md.node)"
            :style="{ width: '250px' }"
            id="overlay_Quyen"
            ref="menuQuyen"
            :popup="true"
            :options="tdQuyens"
            optionLabel="text"
            optionValue="value"
            placeholder="Chọn quyền"
          />
        </template>
      </Column>
    </TreeTable>
    <template #footer>
      <Button
        label="Huỷ"
        icon="pi pi-times"
        @click="closedisplayConfigRole"
        class="p-button-raised p-button-secondary"
      />
      <Button label="Cập nhật" icon="pi pi-save" @click="addConfigRole" />
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
