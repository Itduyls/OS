<script setup>
import { ref, inject, onMounted, watch } from "vue";
import { useToast } from "vue-toastification";
import { required } from "@vuelidate/validators";
import { useVuelidate } from "@vuelidate/core";
//Khai báo
const axios = inject("axios");
const store = inject("store");
const swal = inject("$swal");
const toast = useToast();
const isFirst = ref(true);

const config = {
  headers: { Authorization: `Bearer ${store.getters.token}` },
};

const options = ref({
  IsNext: true,
  sort: "project_id",
  SearchText: "",
  PageNo: 0,
  PageSize: 20,
  FilterUsers_ID: null,
  loading: true,
  totalRecords: null,
});

const listDatabase = ref([]);
const dbSelected = ref();
const projectLogo = ref();
const tableSelected = ref();
const listGroupName = ref();
const groupTable = ref([]);
const loadData = (rf) => {
  (async () => {
    listDatabase.value = [];
    await axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "api_project_database",
          par: [{ par: "db_name", va: dbSelected.value }],
        },

        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];
        dbSelected.value = data[0].db_name;
        projectLogo.value = data[0].project_logo;

        data.forEach((element) => {
          let db = { name: element.db_name, code: element.db_name };
          listDatabase.value.push(db);
        });
      })
      .catch((error) => {
        toast.error("Tải dữ liệu không thành công!");
        options.value.loading = false;

        if (error && error.status === 401) {
          swal.fire({
            title: "Error!",
            text: "Mã token đã hết hạn hoặc không hợp lệ, vui lòng đăng nhập lại!",
            icon: "error",
            confirmButtonText: "OK",
          });
          store.commit("gologout");
        }
      });

    listTable.value = [];
    listGroupName.value = [];
    groupTable.value = [];
    await axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "api_groupname_list",
          par: [{ par: "db_name", va: dbSelected.value }],
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];

        data.forEach((element) => {
          if (element.group_name != null) {
            groupTable.value.push({
              name: element.group_name,
              code: element.group_name,
            });
            listGroupName.value.push(element.group_name);
          }
        });
        listGroupName.value.push("Khác");
      })
      .catch((error) => {
        toast.error("Tải dữ liệu không thành công!");
        options.value.loading = false;
        if (error && error.status === 401) {
          swal.fire({
            title: "Error!",
            text: "Mã token đã hết hạn hoặc không hợp lệ, vui lòng đăng nhập lại!",
            icon: "error",
            confirmButtonText: "OK",
          });
          store.commit("gologout");
        }
      });
    await axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "api_table_list",
          par: [
            { par: "db_name", va: dbSelected.value },
            { par: "search", va: options.value.SearchText },
            { par: "status", va: options.value.Status },
          ],
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];
        let i = 0;
        data.forEach((element) => {
          element.is_order = i + 1;

          i++;
        });
        listTable.value = data;
        renderTree(data);
      })
      .catch((error) => {
        toast.error("Tải dữ liệu không thành công!");
        options.value.loading = false;
        if (error && error.status === 401) {
          swal.fire({
            title: "Error!",
            text: "Mã token đã hết hạn hoặc không hợp lệ, vui lòng đăng nhập lại!",
            icon: "error",
            confirmButtonText: "OK",
          });
          store.commit("gologout");
        }
      });
  })();
};
const datalists = ref();
const renderTree = (data) => {
  let arrChils = [];
  let i = 0;
  listGroupName.value
    .filter((x) => x != null)
    .forEach((m) => {
      let om = { key: m, data: m, label: m };
      if (m != "Khác") {
        let dts = data.filter((x) => x.group_name == m);
        if (dts.length > 0) {
          if (!om.children) om.children = [];
          dts.forEach((em) => {
            em.is_order = i + 1;
            i++;
            let om1 = { key: em.table_id, data: em, label: em.table_name };
            om.children.push(om1);
          });
        }
        arrChils.push(om);
      } else {
        let dsn = data.filter((x) => x.group_name == null);
        if (dsn.length > 0) {
          if (!om.children) om.children = [];
          dsn.forEach((em) => {
            em.is_order = i + 1;
            i++;
            let om1 = { key: em.table_id, data: em, label: em.table_name };
            om.children.push(om1);
          });
          arrChils.push(om);
        }
      }
    });
  datalists.value = arrChils;
};
const loadTable = () => {
  nameTable.value = null;
  desTable.value = null;
  tableID.value = null;
  tableSelected.value = null;
  listCol.value = [];
  listTable.value = [];
  listGroupName.value = [];
  (async () => {
    await axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "api_project_database",
          par: [{ par: "db_name", va: dbSelected.value }],
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];
        projectLogo.value = data[0].project_logo;
      })
      .catch((error) => {
        toast.error("Tải dữ liệu không thành công!");
        options.value.loading = false;

        if (error && error.status === 401) {
          swal.fire({
            title: "Error!",
            text: "Mã token đã hết hạn hoặc không hợp lệ, vui lòng đăng nhập lại!",
            icon: "error",
            confirmButtonText: "OK",
          });
          store.commit("gologout");
        }
      });

    await axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "api_groupname_list",
          par: [{ par: "db_name", va: dbSelected.value }],
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];

        data.forEach((element) => {
          if (element.group_name != null) {
            listGroupName.value.push(element.group_name);
          }
        });
        listGroupName.value.push("Khác");
      });
    await axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "api_table_list",
          par: [
            { par: "db_name", va: dbSelected.value },
            { par: "search", va: options.value.SearchText },
            { par: "status", va: options.value.Status },
          ],
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];
        let i = 0;
        data.forEach((element) => {
          element.is_order = i + 1;
        });

        listTable.value = data;
        renderTree(data);
      })
      .catch((error) => {
        toast.error("Tải dữ liệu không thành công!");
        options.value.loading = false;
        if (error && error.status === 401) {
          swal.fire({
            title: "Error!",
            text: "Mã token đã hết hạn hoặc không hợp lệ, vui lòng đăng nhập lại!",
            icon: "error",
            confirmButtonText: "OK",
          });
          store.commit("gologout");
        }
      });
  })();
};
const listTable = ref([]);
const listCol = ref([]);
function renderCol(data) {
  listCol.value = data;

  listCol.value.forEach((element) => {
    if (element.table_forenkey_id != null) {
      axios
        .post(
          baseURL + "/api/Proc/CallProc",
          {
            proc: "api_col_get",
            par: [{ par: "col_id", va: element.col_forenkey_id }],
          },
          config
        )
        .then((response) => {
          let data = JSON.parse(response.data.data)[0];
          if (!element.col_forenkey_name) element.col_forenkey_name = "";
          element.col_forenkey_name = data[0].col_name;
        });

      listTable.value.forEach((item) => {
        if (item.table_id == element.table_forenkey_id) {
          if (!element.table_forenkey_name) element.table_forenkey_name = "";
          element.table_forenkey_name = item.table_name;
          return;
        }
      });
    }
  });
}
const nameTable = ref("");
const desTable = ref("");
const tableID = ref();
const nodeSelected = ref();
const loadCol = (node) => {
  if (node.data.table_id) {
    nodeSelected.value = node;
    nameTable.value = node.data.table_name;
    desTable.value = node.data.des;
    tableID.value = node.data.table_id;
    axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "api_col_list",
          par: [
            { par: "table_id", va: node.data.table_id },
            { par: "pageno", va: options.value.PageNo },
            { par: "pagesize", va: options.value.PageSize },
            { par: "search", va: options.value.SearchText },
            { par: "status", va: options.value.Status },
          ],
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];
        data.forEach((element, i) => {
          element.is_order = i + 1;
          if (element.col_type.includes("(-1)")) {
            element.col_type = element.col_type.replace("(-1)", "(MAX)");
          }
        });
        renderCol(data);
      })
      .catch((error) => {
        toast.error("Tải dữ liệu không thành công!");
        options.value.loading = false;
        if (error && error.status === 401) {
          swal.fire({
            title: "Error!",
            text: "Mã token đã hết hạn hoặc không hợp lệ, vui lòng đăng nhập lại!",
            icon: "error",
            confirmButtonText: "OK",
          });
          store.commit("gologout");
        }
      });
  }
};
const onCellEditComplete = (event) => {
  let { data, newValue, field } = event;
  data[field] = newValue;

  axios
    .put(baseURL + "/api/api_table/Update_Colum", data, config)
    .then((response) => {
      if (response.data.err != "1") {
        swal.close();
      } else {
        console.log("LỖI A:", response);
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
const searchCol = (event) => {
  if (event.code == "Enter") {
    options.value.loading = true;
    loadCol(nodeSelected.value);
  }
};
const refeshCol = () => {
  loadCol(nodeSelected.value);
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "api_AutoRefershCol",
        par: [{ par: "db_name", va: dbSelected.value }],
      },
      config
    )
    .then(() => {
      toast.success("Cập nhật các trường thành công!");
    })
    .catch((error) => {
      toast.error("Tải dữ liệu không thành công!");
      options.value.loading = false;
      if (error && error.status === 401) {
        swal.fire({
          title: "Error!",
          text: "Mã token đã hết hạn hoặc không hợp lệ, vui lòng đăng nhập lại!",
          icon: "error",
          confirmButtonText: "OK",
        });
        store.commit("gologout");
      }
    });
};
const refeshTable = () => {
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "api_AutoRefershTable",
        par: [{ par: "db_name", va: dbSelected.value }],
      },
      config
    )
    .then(() => {
      toast.success("Cập nhật bảng thành công!");
    });
};

//Xuất excel
const projectButs = ref();
const itemButs = ref([
  {
    label: "Xuất Excel",
    icon: "pi pi-file-excel",
    command: (event) => {
      exportData("ExportExcel");
    },
  },
  {
    label: "Import Excel",
    icon: "pi pi-file-excel",
    command: (event) => {
      exportData("ExportExcel");
    },
  },
]);
const toggleExport = (event) => {
  projectButs.value.toggle(event);
};
const exportData = (method) => {
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  axios
    .post(
      baseURL + "/api/Excel/ExportExcel",
      {
        excelname: "DANH SÁCH TRƯỜNG",
        proc: "api_col_listexport",
        par: [{ par: "table_id", va: tableID.value }],
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
        store.commit("gologout");
      }
    });
};
const headerTable = ref();
const displayTable = ref(false);
const table = ref({
  table_name: "",
  des: "",
  is_order: 1,
  status: true,
});
const openTable = (data) => {
  table.value = data;
  headerTable.value = "Cập nhật bảng";
  displayTable.value = true;
};
const closeTable = () => {
  displayTable.value = false;
  table.value = {
    table_name: "",
    des: "",
    is_order: 1,
    status: true,
  };
};
const saveTable = () => {
  axios
    .put(baseURL + "/api/api_table/Update_Table", table.value, config)
    .then((response) => {
      if (response.data.err != "1") {
        swal.close();
        toast.success("Cập nhật bảng thành công");
        loadData();
        closeTable();
      } else {
        console.log("LỖI A:", response);
        swal.fire({
          title: "Error!",
          text: response.data.ms,
          icon: "error",
          confirmButtonText: "OK",
        });
      }
    })
    .catch((error) => {
      console.log("moe", error);
      swal.close();
      swal.fire({
        title: "Error!",
        text: "Có lỗi xảy ra, vui lòng kiểm tra lại!",
        icon: "error",
        confirmButtonText: "OK",
      });
    });
};
const columns = ref([
  { field: "is_null", header: "Null" },
  { field: "is_key", header: "Key" },
  { field: "is_identity", header: "Identity" },
  { field: "status", header: "Trạng thái" },
  { field: "table_forenkey_name", header: "Bảng liên quan" },
  { field: "col_forenkey_name", header: "Cột liên quan" },
]);
const selectedColumns = ref();
const isCheckTable = ref(true);
const onToggle = (val) => {
  selectedColumns.value = columns.value.filter((col) => val.includes(col));

  if (selectedColumns.value.length == 0) {
    isCheckTable.value = true;
  } else {
    isCheckTable.value = false;
  }
};
const expandedKeys = ref({});
const basedomainURL = baseURL;
const groupName = ref();
const onNodeSelect = (node) => {
  groupName.value = node.key;
  loadCol(node);
  expandedKeys.value[node.key] = true;
};
const onNodeUnselect = (node) => {
  expandedKeys.value[node.key] = false;
};
const checkEditGroupName = ref();
const editGroupName = (data) => {
  groupNameNew.value = null;
  checkEditGroupName.value = data.key;
};
const cancelEditGroupName=(value)=>{
  checkEditGroupName.value="1";

}
const saveGroupName = (oldGroupName) => {
  let check=false;
   expandedKeys.value[oldGroupName.key] = false;
  listGroupName.value.forEach((element) => {
    if (element == groupNameNew.value && groupNameNew.value!=oldGroupName.key) {
      toast.warning("Đã tồn tại tên nhóm.");
      check=true;
      return
    }
  });
  if(check==true){
    return;
  }
  if (groupNameNew.value == "") {
    groupNameNew.value = null;
  }
  let groupNameC={
    groupNameOld:oldGroupName.key,
    groupNameNew:groupNameNew.value
  }
   axios
    .put(baseURL + "/api/api_table/Update_Groupname", groupNameC, config)
    .then((response) => {
      if (response.data.err != "1") {
        swal.close();
        toast.success("Cập nhật bảng thành công");
        loadData();

      } else {
        console.log("LỖI A:", response);
        swal.fire({
          title: "Error!",
          text: response.data.ms,
          icon: "error",
          confirmButtonText: "OK",
        });
      }
    })
    .catch((error) => {
      console.log("moe", error);
      swal.close();
      swal.fire({
        title: "Error!",
        text: "Có lỗi xảy ra, vui lòng kiểm tra lại!",
        icon: "error",
        confirmButtonText: "OK",
      });
    });
};
const groupNameNew = ref("");
onMounted(() => {
  store.commit("setisadmin", true);
  loadData();
  return {
    loadData,
  };
});
</script>
<template>
  <div class="surface-100 w-full">
    <div class="w-full">
      <Splitter class="w-full">
        <SplitterPanel :size="20">
          <div class="m-3 mr-0 flex">
            <div>
              <img
                :src="
                  projectLogo
                    ? basedomainURL + projectLogo
                    : '/src/assets/image/noimg.jpg'
                "
                alt=""
                class="p-0 pr-2"
                width="45"
                height="40"
              />
            </div>
            <Dropdown
              v-model="dbSelected"
              :options="listDatabase"
              optionLabel="name"
              optionValue="code"
              placeholder="Chọn database"
              class="w-full"
              @change="loadTable"
            >
            </Dropdown>
            <Button
              class="w-2 ml-2 p-button-outlined p-button-secondary"
              icon="pi pi-refresh"
              @click="refeshTable"
            />
          </div>
          <div style="height: calc(100vh - 130px)">
            <Tree
              :value="datalists"
              selectionMode="single"
              v-model:selectionKeys="tableSelected"
              :metaKeySelection="false"
              class="h-full w-full overflow-x-hidden"
              scrollHeight="flex"
              :expandedKeys="expandedKeys"
              @nodeSelect="onNodeSelect"
              @nodeUnselect="onNodeUnselect"
            >
              <template #default="slotProps">
                <div class="flex">
                  <div
                    class="country-item flex w-full pt-2"
                    v-if="slotProps.node.children"
                  >
                    <img
                      src="../../assets/image/folder.png"
                      width="28"
                      height="36"
                      style="object-fit: contain"
                    />
                    <div>
                      <div
                        v-if="checkEditGroupName != slotProps.node.key"
                        class="px-2 text-lg"
                        style="line-height: 36px"
                      >
                        {{ slotProps.node.label }} ({{slotProps.node.children.length}})
                      </div>
                      <div v-else class="flex">
                        <InputText
                          @keyup.enter="saveGroupName(slotProps.node)"
                          v-model="groupNameNew"
                          spellcheck="false"
                          autofocus="true"
                          class="m-2"
                        ></InputText>
                          <Button
                        
                        class="
                          p-button-rounded p-button-secondary p-button-outlined
                          m-2
                        "
                        type="button"
                        icon="pi pi-times"
                        @click="cancelEditGroupName(slotProps.node.key)"
                      ></Button>
                      </div>
                    </div>

                    <div
                      class="w-2 pt-1"
                      v-if="groupName == slotProps.node.key"
                    >
               
                      <Button
                        v-if="checkEditGroupName != slotProps.node.key" 
                        @click="editGroupName(slotProps.node)"
                        class="
                          p-button-rounded p-button-secondary p-button-outlined
                          border-none
                          mx-1 mb-2
                        "
                        type="button"
                        icon="pi pi-pencil"
                      ></Button>
                     
                  
                    </div>
                  </div>
                  <div class="country-item flex w-full pt-2" v-else>
                    <div class="w-full">
                      <div class="flex">
                        <i
                          class="pi pi-table pt-1"
                          style="font-size: 1.125rem"
                        ></i>
                        <div class="px-2 text-lg">
                          {{ slotProps.node.label }}
                        </div>
                      </div>
                      <div class="flex">
                        <i
                          class="pi pi-table"
                          style="font-size: 1.125rem; color: transparent"
                        ></i>
                        <small class="px-2 font-italic">
                          {{ slotProps.node.data.des }}
                        </small>
                      </div>
                    </div>
                    <div
                      class="w-2 pt-1"
                      v-if="tableID == slotProps.node.data.table_id"
                    >
                      <Button
                        @click="openTable(slotProps.node.data)"
                        class="
                          p-button-rounded p-button-secondary p-button-outlined
                          mx-1
                        "
                        type="button"
                        icon="pi pi-pencil"
                      ></Button>
                    </div>
                  </div>
                </div>
              </template>
            </Tree>
          </div>
        </SplitterPanel>
        <SplitterPanel :size="80">
          <div>
            <div class="h-2rem p-3 pb-0 mb-0 surface-0 w-full">
              <h3 class="m-0 text-primary">
                <i class="pi pi-table" style="font-size: 1rem"></i>
                {{ nameTable ? nameTable : "Chưa chọn bảng" }}
                <Chip :label="desTable" class="mr-2 mb-2 custom-chip" />
              </h3>
            </div>
            <Toolbar class="outline-none mr-3 surface-0 border-none mt-1">
              <template #start>
                <span class="p-input-icon-left">
                  <i class="pi pi-search" />
                  <InputText
                    v-model="options.SearchText"
                    @keyup="searchCol"
                    type="text"
                    spellcheck="false"
                    placeholder="Tìm kiếm"
                  />
                </span>
              </template>

              <template #end>
                <MultiSelect
                  :modelValue="selectedColumns"
                  :options="columns"
                  optionLabel="header"
                  class="mx-2"
                  placeholder="Select Columns"
                  style="width: 10em"
                  @update:modelValue="onToggle"
                />
                <Button
                  class="mr-2 p-button-outlined p-button-secondary"
                  icon="pi pi-refresh"
                  @click="refeshCol"
                />

                <Button
                  label="Tiện ích"
                  icon="pi pi-file-excel"
                  class="mr-2 p-button-outlined p-button-secondary"
                  aria-haspopup="true"
                  aria-controls="overlay_Export"
                  @click="toggleExport"
                />
                <Menu
                  id="overlay_Export"
                  ref="projectButs"
                  :popup="true"
                  :model="itemButs"
                />
              </template>
            </Toolbar>
            <div class="d-lang-table mx-3">
              <DataTable
                :value="listCol"
                :scrollable="true"
                scrollHeight="flex"
                :lazy="true"
                :rowHover="true"
                filterDisplay="menu"
                :showGridlines="true"
                responsiveLayout="scroll"
                editMode="cell"
                @cell-edit-complete="onCellEditComplete"
                data-key="col_id"
              >
                <Column
                  field="is_order"
                  header="STT"
                  headerStyle="text-align:center;max-width:75px;height:50px"
                  bodyStyle="text-align:center;max-width:75px;;max-height:60px"
                  class="align-items-center justify-content-center text-center"
                >
                </Column>

                <Column
                  field="col_name"
                  header="Tên cột"
                  headerStyle="max-width:200px;height:50px"
                  bodyStyle="max-width:200px;max-height:60px"
                >
                </Column>
                <Column
                  field="col_type"
                  header="Kiểu dữ liệu"
                  headerStyle="text-align:center;max-width:150px;height:50px"
                  bodyStyle="text-align:center;max-width:150px;;max-height:60px"
                  class="align-items-center justify-content-center text-center"
                >
                </Column>
                <Column
                  field="des"
                  header="Mô tả"
                  headerStyle="text-align:center;height:50px; padding:0"
                  bodyStyle="text-align:center;max-height:60px; padding:0"
                  class="px-2"
                  v-if="isCheckTable"
                >
                  <template #editor="{ data, field }">
                    <Textarea
                      v-model="data[field]"
                      autofocus
                      class="w-full p-0 h-full surface-200"
                    />
                  </template>
                </Column>
                <Column
                  headerStyle="text-align:center;max-width:150px;height:50px"
                  bodyStyle="text-align:center;max-width:150px;;max-height:60px"
                  class="align-items-center justify-content-center text-center"
                  v-for="(col, index) of selectedColumns"
                  :field="col.field"
                  :header="col.header"
                >
                  <template
                    v-if="
                      col.field == 'is_key' ||
                      col.field == 'is_identity' ||
                      col.field == 'is_null' ||
                      col.field == 'status'
                    "
                    #body="data"
                  >
                    <Checkbox
                      :binary="data.data[col.field]"
                      v-model="data.data[col.field]"
                      disabled="true"
                    />
                  </template>
                </Column>

                <template #empty>
                  <div
                    class="
                      align-items-center
                      justify-content-center
                      p-4
                      text-center
                      m-auto
                    "
                    v-if="!isFirst"
                  >
                    <img
                      src="../../assets/background/nodata.png"
                      height="144"
                    />
                    <h3 class="m-1">Không có dữ liệu</h3>
                  </div>
                </template>
              </DataTable>
            </div>
          </div>
        </SplitterPanel>
      </Splitter>
    </div>
  </div>
  <Dialog
    :header="headerTable"
    v-model:visible="displayTable"
    :style="{ width: '40vw' }"
  >
    <form>
      <div class="grid formgrid m-2">
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left p-0">Tên bảng </label>
          <InputText
            v-model="table.table_name"
            spellcheck="false"
            class="col-10 ip36 px-2"
            :disabled="true"
          />
        </div>
        <div style="display: flex" class="col-12 field md:col-12">
          <div class="field col-4 md:col-4 p-0">
            <label class="col-6 text-left p-0">Số thứ tự </label>
            <InputNumber v-model="table.is_order" class="col-6 ip36 p-0" />
          </div>
          <div class="field col-6 md:col-6 p-0">
            <label
              style="vertical-align: text-bottom"
              class="col-6 text-center p-0"
              >Trạng thái
            </label>
            <InputSwitch v-model="table.status" class="col-6" />
          </div>
        </div>
        <div class="field col-12 md:col-12 flex">
          <label class="col-2 text-left p-0">Mô tả</label>
          <div class="col-10 p-0">
            <Textarea
            spellcheck="false"
              v-model="table.des"
              class="col-12 ip36"
              autoResize
              autofocus
            />
          </div>
        </div>
        <div class="field col-12 md:col-12 flex">
          <label class="col-2 text-left p-0">Nhóm</label>
          <div class="col-10 p-0">
            <Dropdown spellcheck="false"
              v-model="table.group_name"
              :options="groupTable"
              optionLabel="name"
              optionValue="code"
              :editable="true"
            />
          </div>
        </div>
      </div>
    </form>
    <template #footer>
      <Button
        label="Hủy"
        icon="pi pi-times"
        @click="closeTable"
        class="p-button-text"
      />

      <Button label="Lưu" icon="pi pi-check" @click="saveTable()" />
    </template>
  </Dialog>
</template>

<style scoped>
.d-lang-table {
  height: calc(100vh - 170px);
}
.d-table-container {
  height: calc(100vh - 500px);
}
.d-btn-function {
  border-radius: 50%;
  margin-left: 6px;
}
.inputanh {
  border: 1px solid #ccc;
  width: 96px;
  height: 96px;
  cursor: pointer;
  padding: 1px;
}
.ipnone {
  display: none;
}
.inputanh img {
  object-fit: cover;
  width: 100%;
  height: 100%;
}
</style>