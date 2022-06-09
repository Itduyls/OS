<script setup>
import { ref, inject, onMounted } from "vue";
import { required } from "@vuelidate/validators";
import { useToast } from "vue-toastification";
import { useVuelidate } from "@vuelidate/core";
//Khai báo biến
const isFirst = ref(true);
const table = ref({});
const tables = ref([]);
const columns = ref([]);
const databases = ref({});
const displayUpdateColumn = ref(false);
const store = inject("store");
const toast = useToast();
const swal = inject("$swal");
const axios = inject("axios"); // inject axios
const basedomainURL = baseURL;
const config = {
  headers: { Authorization: `Bearer ${store.getters.token}` },
};
const oInputs = [
  { value: "Number", text: "Number" }, //0
  { value: "Text", text: "Text" }, //1
  { value: "TextArea", text: "TextArea" }, //2
  { value: "Editor", text: "CKEditor" }, //3
  { value: "Date", text: "Date" }, //4
  { value: "DateTime", text: "DateTime" }, //5
  { value: "Time", text: "Time" }, //6
  { value: "Select", text: "Select" }, //7
  { value: "Combobox", text: "Combobox" }, //8
  { value: "CheckBox", text: "CheckBox" }, //9
  { value: "Radio", text: "Radio" }, //10
  { value: "Switch", text: "Switch" }, //11
  { value: "File", text: "File" }, //12
  { value: "Image", text: "Image" }, //13
  { value: "Avarta", text: "Avarta" }, //14
  { value: "Color", text: "Color" }, //15
];
const oCSS = [
  { value: "col-2", text: "col-2" }, //0
  { value: "col-3", text: "col-3" }, //1
  { value: "col-4", text: "col-4" }, //2
  { value: "col-6", text: "col-6" }, //3
  { value: "col-12", text: "col-12" }, //4
];
//Khai báo function
//Show Modal
const showModalUpdateColumn = () => {
  displayUpdateColumn.value = true;
};
const closedisplayUpdateColumn = () => {
  displayUpdateColumn.value = false;
};
//Thêm sửa xoá
const onRefersh = () => {
  goTable();
};
const loading = ref(false);
const tdCols = ref([]);
const changTable = (tb) => {
  axios
    .get(baseURL + "/api/Auto/List_Columns?table=" + tb + "&connect=" + connectString, {
      headers: { Authorization: `Bearer ${store.getters.token}` },
    })
    .then((response) => {
      let data = JSON.parse(response.data.data)[0];
      if (data.length > 0) {
        tdCols.value = data;
      } else {
        tdCols.value = [];
      }
    });
};
const goTable = (tb) => {
  activeTab.value = 0;
  if (!tb.value) return false;
  loading.value = true;
  axios
    .get(
      baseURL +
        "/api/Auto/List_Columns?table=" +
        tb.value.TABLE_NAME +
        "&connect=" +
        connectString,
      {
        headers: { Authorization: `Bearer ${store.getters.token}` },
      }
    )
    .then((response) => {
      loading.value = false;
      let data = JSON.parse(response.data.data)[0];
      if (data.length > 0) {
        data.forEach((c) => {
          if (c.Name == "Maunen" || c.Name == "Mauchu") {
            c.Input = oInputs[15].value;
          } else if (c.CType.includes("varchar")) {
            if (c.CLength == -1) {
              c.Input = oInputs[2].value;
            } else {
              c.Input = oInputs[1].value;
            }
          } else if (c.CType == "float" || c.CType == "int") {
            c.Input = oInputs[0].value;
          } else if (c.CType == "bit") {
            c.Input = oInputs[9].value;
          } else if (c.CType == "datetime") {
            c.Input = oInputs[5].value;
          } else if (c.CType == "date") {
            c.Input = oInputs[4].value;
          } else if (c.CType == "time") {
            c.Input = oInputs[6].value;
          }
          c.Css = "col-12";
        });
        columns.value = data;
      } else {
        columns.value = [];
      }
      //Info table
      getinfoTable();
    })
    .catch((error) => {
      loading.value = false;
      if (error && error.status === 401) {
        errorMessage();
      }
    });
};
let connectString = "";
const loadDatabase = () => {
  connectString = localStorage.getItem("database");
  if (connectString) {
    databases.value = JSON.parse(window.atob(connectString));
    databases.value.new = false;
    loadTable();
  } else {
    showConnectDatabase();
  }
};
const errorMessage = () => {
  swal.fire({
    title: "Error!",
    text: "Mã token đã hết hạn hoặc không hợp lệ, vui lòng đăng nhập lại!",
    icon: "error",
    confirmButtonText: "OK",
  });
};
const swalMessage = (title, icon, ms) => {
  swal.fire({
    title: title,
    text: ms,
    icon: icon,
    confirmButtonText: "OK",
  });
};
const loadTable = async () => {
  swalLoadding();
  var response = await axios
    .get(baseURL + "/api/Auto/List_Tables?connect=" + connectString, config)
    .catch((error) => {
      return error;
    });
  swal.close();
  if (response.status == 200) {
    let data = JSON.parse(response.data.data)[0];
    if (data.length > 0) {
      tables.value = data;
    } else {
      tables.value = [];
    }
  } else if (response.status == 401) {
    errorMessage();
  }
};
const saveColumn = () => {
  swalLoadding();
  axios
    .post(baseURL + "/api/Auto/Add_Tables", columns.value, config)
    .then((response) => {
      swal.close();
      if (response.data.err != "1") {
        swal.close();
        toast.success("Cập nhật Table thành công!");
        goTable();
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
        errorMessage();
      }
    });
};
const connect = () => {
  if (
    !databases.value.InitialCatalog ||
    !databases.value.DataSource ||
    !databases.value.UserId ||
    !databases.value.Password
  ) {
    swalMessage("Error!", "error", "Vui lòng nhập đầy đủ thông tin kết nối Database!");
    return false;
  }
  connectString = window.btoa(JSON.stringify(databases.value));
  localStorage.setItem("database", connectString);
  databases.value.new = false;
  loadTable();
  closedisplayConnectDatabase();
  columns.value = [];
};
const exportTable = (method) => {
  swalLoadding();
  axios
    .post(
      baseURL + "/api/Excel/ExportExcelTable",
      {
        excelname: "TABLE",
        proc: "Sys_Tables_ListExport",
        par: [],
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
        swalMessage("Error!", "error", response.data.ms);
      }
    })
    .catch((error) => {
      if (error.status === 401) {
        errorMessage();
      }
    });
};
//Diagog
const activeTab = ref(0);
const displayConnectDatabase = ref(false);
const showConnectDatabase = () => {
  displayConnectDatabase.value = true;
};
const closedisplayConnectDatabase = () => {
  displayConnectDatabase.value = false;
};
//Table
const swalLoadding = () => {
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
};
const displayTable = ref(false);
const getinfoTable = async () => {
  swalLoadding();
  var response = await axios
    .get(
      baseURL +
        "/api/Auto/Get_TableDes?dbname=" +
        databases.value.InitialCatalog +
        "&table=" +
        table.value.TABLE_NAME,
      config
    )
    .catch((error) => {
      return error;
    });
  swal.close();
  if (response.status == 200) {
    let data = JSON.parse(response.data.data);
    if (data.length > 0) {
      if (data.length > 1) {
        for (var key in data[1][0]) {
          table.value[key] = data[1][0][key];
        }
        data[0].forEach((e) => {
          let cl = columns.value.find((x) => x.Name == e.FKCOLUMN_NAME);
          if (cl) {
            cl.ReTable = e.PKTABLE_NAME;
            cl.ReCol = e.PKCOLUMN_NAME;
          }
        });
      }
    } else {
      table.value.ID = -1;
      table.value.DBName = table.value.TABLE_CATALOG;
      table.value.TableName = table.value.TABLE_NAME;
    }
  }
};
const showTable = async () => {
  displayTable.value = true;
};
const closedisplayTable = () => {
  displayTable.value = false;
};
const saveTable = async () => {
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  var response = await axios
    .post(baseURL + "/api/Auto/Add_TableDes", table.value, config)
    .catch((error) => {
      return error;
    });

  swal.close();
  if (response.status == 200) {
    if (response.data.err != "1") {
      swal.close();
      toast.success("Cập nhật thông tin table thành công!");
      closedisplayTable();
    } else {
      swalMessage("Error!", "error", response.data.ms);
    }
  } else if (response.status == 401) {
    errorMessage();
  }
};
onMounted(() => {
  loadDatabase();
  return {};
});
</script>
<template>
  <div
    class="main-layout h-full w-full p-2"
    v-if="store.getters.islogin && !databases.new && databases.new != null"
    style="display: grid"
  >
    <Splitter>
      <SplitterPanel
        class="shadow-1 p-2"
        style="
          width: 251px;
          min-width: 210px;
          max-width: 210px;
          background-color: #fff;
          flex-basis: unset;
          flex-grow: unset;
        "
      >
        <h3 class="m-0">
          <Button class="p-button-rounded p-button-text" @click="showConnectDatabase">
            <i class="pi pi-database mr-2 ml-2"></i> <b>{{ databases.InitialCatalog }}</b>
          </Button>
        </h3>
        <ScrollPanel
          class="m-2"
          style="width: 100%; height: calc(100% - 200px); overflow-y: auto"
        >
          <Listbox
            v-model="table"
            emptyMessage=""
            :filter="true"
            filterPlaceholder="Tìm theo tên"
            :options="tables"
            @change="goTable"
            optionLabel="TABLE_NAME"
            style="border: 0"
            class="listbox-tables"
          >
            <template #option="slotProps">
              <div>
                <i class="pi pi-table"></i>
                <span
                  class="ml-2"
                  :style="{ fontWeight: slotProps.option.IsEdit ? 'bold' : 'normal' }"
                  >{{ slotProps.option.TABLE_NAME }}</span
                >
              </div>
            </template>
          </Listbox>
        </ScrollPanel>
      </SplitterPanel>
      <SplitterPanel
        class="flex-grow-1 p-2"
        style="margin-left: 1.5px; background-color: #fff"
      >
        <div
          class="flex justify-content-center align-items-center"
          v-if="columns.length > 0"
        >
          <Toolbar class="w-full custoolbar">
            <template #start>
              <h3 class="module-title mt-0 ml-1 mb-0" v-if="table">
                <i class="pi pi-table"></i> <span>{{ table.TABLE_NAME }}</span>
                <Chip
                  class="ml-2"
                  style="background-color: #4285f4; color: #fff"
                  :label="table.Title"
                />
              </h3>
              <Button
                @click="showTable"
                icon="pi pi-pencil"
                class="p-button-rounded p-button-text ml-2"
              />
              <div style="font-size: 13px">
                <i>{{ table.Des }}</i>
              </div>
            </template>
            <template #end>
              <Button
                class="mr-2 p-button-outlined p-button-secondary"
                icon="pi pi-refresh"
                @click="onRefersh"
              />
              <Button
                label="Export"
                icon="pi pi-file-excel"
                class="mr-2 p-button-outlined p-button-secondary"
                @click="exportTable"
                aria-haspopup="true"
                aria-controls="overlay_Export"
              />
              <Button label="Cập nhật" icon="pi pi-save" @click="saveColumn" />
            </template>
          </Toolbar>
        </div>
        <TabView ref="tabview" class="no-pad" v-model:activeIndex="activeTab">
          <TabPanel header="Thông tin Table">
            <DataTable
              class="w-full p-datatable-sm e-sm"
              :lazy="true"
              :value="columns"
              :loading="loading"
              dataKey="Name"
              :rowHover="true"
              :resizableColumns="true"
              :reorderableColumns="true"
              columnResizeMode="fit"
              :showGridlines="true"
              :scrollable="true"
              scrollHeight="calc(100vh - 200px)"
            >
              <Column
                field="Name"
                frozen
                header="Tên cột"
                class="align-items-center justify-content-center"
                :style="{ maxWidth: '180px' }"
                headerStyle="color:#fff;background-color: #4285f4;"
                bodyStyle="font-weight:500;background-color: aliceblue;"
              ></Column>
              <Column
                field="Title"
                header="Tiêu đề"
                class="align-items-center justify-content-center"
                :style="{ maxWidth: '180px' }"
                headerStyle="color:#fff;background-color: #4285f4;"
              >
              </Column>
              <Column
                field="CType"
                header="Kiểu dữ liệu"
                class="align-items-center justify-content-center"
                :style="{ maxWidth: '130px' }"
                headerStyle="color:#fff;background-color: #4285f4;"
              >
                <template #body="slotProps">
                  <span>{{ slotProps.data.CType }}</span
                  ><span v-if="slotProps.data.CLength">
                    ({{
                      slotProps.data.CLength == -1 ? "MAX" : slotProps.data.CLength
                    }})</span
                  >
                </template>
              </Column>
              <Column
                field="Required"
                header="NULL"
                class="align-items-center justify-content-center"
                :style="{ maxWidth:'75px' }"
                headerStyle="color:#fff;background-color: #4285f4;"
              >
                <template #body="slotProps">
                  <Checkbox
                    id="Required"
                    v-model="slotProps.data.Required"
                    :binary="true"
                  />
                </template>
              </Column>
              <Column
                field="Des"
                header="Mô tả"
                class="align-items-center justify-content-center"
                headerStyle="color:#fff;background-color: #4285f4;"
              >
                <template #body="slotProps">
                  <Textarea
                    style="border: none; resize: none"
                    class="w-full"
                    spellcheck="false"
                    type="text"
                    v-model="slotProps.data.Des"
                  />
                </template>
              </Column>
              <Column
                field="ReTable"
                header="Bảng liên quan"
                class="align-items-center justify-content-center"
                :style="{ maxWidth: '150px' }"
                headerStyle="color:#fff;background-color: #4285f4;"
              >
              </Column>
              <Column
                field="ReCol"
                header="Cột liên quan"
                class="align-items-center justify-content-center"
                :style="{ maxWidth: '150px' }"
                headerStyle="color:#fff;background-color: #4285f4;"
              >
              </Column>
              <template #empty>
                <div
                  class="align-items-center justify-content-center p-4 text-center m-auto"
                  v-if="!isFirst"
                >
                  <img src="../../assets/background/nodata.png" height="144" />
                  <h3 class="m-1">Không có dữ liệu</h3>
                </div>
              </template>
            </DataTable>
          </TabPanel>
          <TabPanel header="Thông tin Form">
            <DataTable
              class="w-full p-datatable-sm e-sm"
              :lazy="true"
              :value="columns"
              :loading="loading"
              dataKey="Name"
              :rowHover="true"
              :resizableColumns="true"
              columnResizeMode="fit"
              :showGridlines="true"
              responsiveLayout="scroll"
              :scrollable="true"
              scrollHeight="calc(100vh - 200px)"
            >
              <Column
                field="Name"
                frozen
                header="Tên cột"
                class="align-items-center justify-content-center"
                :style="{ maxWidth: '180px' }"
                headerStyle="color:#fff;background-color: #4285f4;"
                bodyStyle="font-weight:500;background-color: aliceblue;"
              ></Column>
              <Column
                field="Title"
                header="Tiêu đề"
                class="align-items-center justify-content-center text-center"
                headerStyle="text-align:center;color:#fff;background-color: #4285f4;"
                bodyStyle="text-align:center;"
              >
                <template #body="slotProps">
                  <InputText
                    spellcheck="false"
                    class="w-full"
                    type="text"
                    v-model="slotProps.data.Title"
                  />
                </template>
              </Column>
              <Column
                field="CType"
                header="Kiểu dữ liệu"
                class="align-items-center justify-content-center text-center"
                headerStyle="text-align:center;max-width:130px;color:#fff;background-color: #4285f4;"
                bodyStyle="text-align:center;max-width:130px"
              >
                <template #body="slotProps">
                  <span>{{ slotProps.data.CType }}</span
                  ><span v-if="slotProps.data.CLength">
                    ({{
                      slotProps.data.CLength == -1 ? "MAX" : slotProps.data.CLength
                    }})</span
                  >
                </template>
              </Column>
              <Column
                field="Input"
                header="Kiểu Form"
                class="align-items-center justify-content-center text-center"
                headerStyle="text-align:center;max-width:150px;color:#fff;background-color: #4285f4;"
                bodyStyle="text-align:center;max-width:150px"
              >
                <template #body="slotProps">
                  <Dropdown
                    class="w-full"
                    v-model="slotProps.data.Input"
                    :filter="true"
                    :options="oInputs"
                    optionLabel="text"
                    optionValue="value"
                  />
                </template>
              </Column>
              <Column
                field="Required"
                header="NULL"
                class="align-items-center justify-content-center text-center"
                headerStyle="text-align:center;max-width:75px;color:#fff;background-color: #4285f4;"
                bodyStyle="text-align:center;max-width:75px"
              >
                <template #body="slotProps">
                  <Checkbox
                    id="Required"
                    v-model="slotProps.data.Required"
                    :binary="true"
                  />
                </template>
              </Column>
              <Column
                field="Show"
                header="Hiển thị bảng"
                class="align-items-center justify-content-center text-center"
                headerStyle="text-align:center;max-width:80px;color:#fff;background-color: #4285f4;"
                bodyStyle="text-align:center;max-width:80px"
              >
                <template #body="slotProps">
                  <Checkbox id="Show" v-model="slotProps.data.Show" :binary="true" />
                </template>
              </Column>
              <Column
                field="ShowForm"
                header="Hiển thị Form"
                class="align-items-center justify-content-center text-center"
                headerStyle="text-align:center;max-width:80px;color:#fff;background-color: #4285f4;"
                bodyStyle="text-align:center;max-width:80px"
              >
                <template #body="slotProps">
                  <Checkbox
                    id="ShowForm"
                    v-model="slotProps.data.ShoShowFormw"
                    :binary="true"
                  />
                </template>
              </Column>
              <Column
                field="Css"
                header="CSS Form"
                class="align-items-center justify-content-center text-center"
                headerStyle="text-align:center;max-width:120px;color:#fff;background-color: #4285f4;"
                bodyStyle="text-align:center;max-width:120px"
              >
                <template #body="slotProps">
                  <Dropdown
                    class="w-full"
                    v-model="slotProps.data.Css"
                    :options="oCSS"
                    optionLabel="text"
                    optionValue="value"
                  />
                </template>
              </Column>
              <Column
                field="ReTable"
                header="Bảng liên quan"
                class="align-items-center justify-content-center text-center"
                headerStyle="text-align:center;max-width:180px;color:#fff;background-color: #4285f4;"
                bodyStyle="text-align:center;max-width:180px"
              >
                <template #body="slotProps">
                  <Dropdown
                    class="w-full"
                    :filter="true"
                    :showClear="true"
                    v-model="slotProps.data.ReTable"
                    :options="tables"
                    optionLabel="TABLE_NAME"
                    optionValue="TABLE_NAME"
                  >
                  </Dropdown>
                </template>
              </Column>
              <Column
                field="ReCol"
                header="Cột liên quan"
                class="align-items-center justify-content-center text-center"
                headerStyle="text-align:center;max-width:180px;color:#fff;background-color: #4285f4;"
                bodyStyle="text-align:center;max-width:180px"
              >
                <template #body="slotProps">
                  <Dropdown
                    class="w-full"
                    @before-show="changTable(slotProps.data.ReTable)"
                    v-model="slotProps.data.ReCol"
                    :filter="true"
                    :editable="true"
                    :showClear="true"
                    :options="tdCols"
                    optionLabel="Name"
                    optionValue="Name"
                  />
                </template>
              </Column>
              <template #empty>
                <div
                  class="align-items-center justify-content-center p-4 text-center m-auto"
                  v-if="!isFirst"
                >
                  <img src="../../assets/background/nodata.png" height="144" />
                  <h3 class="m-1">Không có dữ liệu</h3>
                </div>
              </template>
            </DataTable>
          </TabPanel>
          <TabPanel header="Thiết lập">
            <div class="divcode">
              <code></code>
            </div>
          </TabPanel>
          <TabPanel header="Design">
            <div class="divcode">
              <code></code>
            </div>
          </TabPanel>
          <TabPanel header="VUE"
            ><div class="divcode">
              <code></code>
            </div>
          </TabPanel>
          <TabPanel header="API"
            ><div class="divcode">
              <code></code>
            </div>
          </TabPanel>
          <TabPanel header="SQL"
            ><div class="divcode">
              <code></code>
            </div>
          </TabPanel>
          <TabPanel header="Test Case"
            ><div class="divcode">
              <code></code>
            </div>
          </TabPanel>
          <TabPanel header="Flutter View"
            ><div class="divcode">
              <code></code>
            </div>
          </TabPanel>
          <TabPanel header="Flutter Controllers"
            ><div class="divcode">
              <code></code>
            </div>
            ></TabPanel
          >
        </TabView>
      </SplitterPanel>
    </Splitter>
  </div>
  <Dialog
    header="Kết nối Server"
    v-model:visible="displayConnectDatabase"
    :style="{ width: '480px', zIndex: 1000 }"
    :closable="false"
    :closeOnEscape="false"
    :autoZIndex="false"
    :modal="true"
  >
    <template #header>
      <h3 class="m-0 p-0"><i class="pi pi-server"></i> Kết nối Server</h3>
    </template>
    <form @submit.prevent="connect">
      <div class="grid formgrid m-2">
        <div class="field col-12 md:col-12">
          <label class="col-4 text-left">Server Name</label>
          <InputText
            spellcheck="false"
            class="col-8 ip36 p-2"
            v-model="databases.DataSource"
          />
        </div>
        <div class="field col-12 md:col-12">
          <label class="col-4 text-left">Database Name</label>
          <InputText
            spellcheck="false"
            class="col-8 ip36 p-2"
            v-model="databases.InitialCatalog"
          />
        </div>
        <div class="field col-12 md:col-12">
          <label class="col-4 text-left">Login</label>
          <InputText
            spellcheck="false"
            class="col-8 ip36 p-2"
            v-model="databases.UserId"
          />
        </div>
        <div class="field col-12 md:col-12">
          <label class="col-4 text-left">PassWord</label>
          <InputText
            type="password"
            spellcheck="false"
            class="col-8 ip36 p-2"
            v-model="databases.Password"
          />
        </div>
      </div>
    </form>
    <template #footer>
      <Button
        label="Huỷ"
        icon="pi pi-times"
        v-if="databases.new != null"
        @click="closedisplayConnectDatabase"
        class="p-button-raised p-button-secondary"
      />
      <Button label="Cập nhật" icon="pi pi-save" @click="connect" />
    </template>
  </Dialog>
  <Dialog
    :header="'Chỉnh sửa thông tin Table ' + table.TABLE_NAME"
    v-model:visible="displayTable"
    :style="{ width: '720px', zIndex: 1000 }"
    :closeOnEscape="false"
    :autoZIndex="false"
    :maximizable="true"
    :modal="true"
  >
    <form @submit.prevent="saveTable">
      <div class="grid formgrid m-2">
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left">Tên bảng</label>
          <InputText spellcheck="false" class="col-10 ip36 p-2" v-model="table.Title" />
        </div>
        <div class="field col-12 md:col-12">
          <label style="vertical-align: top" class="col-2 text-left">Mô tả</label>
          <Textarea spellcheck="false" class="col-10 p-2" rows="5" v-model="table.Des" />
        </div>
      </div>
    </form>
    <template #footer>
      <Button
        label="Huỷ"
        icon="pi pi-times"
        v-if="databases.new != null"
        @click="closedisplayTable"
        class="p-button-raised p-button-secondary"
      />
      <Button label="Cập nhật" icon="pi pi-save" @click="saveTable" />
    </template>
  </Dialog>
</template>
<style>
.listbox-tables .p-listbox-header {
  padding: 0;
}
.divcode {
  background: rgba(0, 0, 0, 0.8);
  color: rgb(255, 255, 255);
  padding: 10px;
  overflow: auto;
  height: calc(100vh - 200px);
}
</style>
