<script setup>
import { ref, inject, onMounted } from "vue";
import { useToast } from "vue-toastification";
import { FilterMatchMode, FilterOperator } from "primevue/api";
import moment from "moment";
//Khai báo biến
const filters = ref({
  Users_ID: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.STARTS_WITH }],
  },
  logdate: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }],
  },
  IP: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.STARTS_WITH }],
  },
  loai: { value: null, matchMode: FilterMatchMode.EQUALS },
  FullName: { value: null, matchMode: FilterMatchMode.IN },
  title: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.STARTS_WITH }],
  },
});
const tdLoais = ref([
  { id: 0, name: "Lỗi" },
  { id: 1, name: "Log" },
  { id: 2, name: "Lỗi Console" },
]);
const modelview = ref({});
const datalists = ref();
const tdUsers = ref([]);
const displayAddData = ref(false);
const isFirst = ref(true);
const filterSQL = ref([]);
const isDynamicSQL = ref(true); //phân trang bình thường hay phân trang tối ưu cho dữ liệu lớn
const toast = useToast();
const swal = inject("$swal");
const store = inject("store");
const axios = inject("axios"); // inject axios
const opition = ref({
  IsNext: true,
  sort: "id DESC",
  PageNo: 0,
  PageSize: 20,
  FilterUsers_ID: null,
  Users_ID: store.getters.Users_ID,
});
const basedomainURL = baseURL;
const config = {
  headers: { Authorization: `Bearer ${store.getters.token}` },
};
const menuButs = ref();
const itemButs = ref([
  {
    label: "Xuất Excel",
    icon: "pi pi-file-excel",
    command: (event) => {
      exportData("ExportExcel");
    },
  },
]);

//Khai báo function
const toggleExport = (event) => {
  menuButs.value.toggle(event);
};
//Show Modal
const showModalDetail = (md) => {
  if (md.logcontent != null) {
    showDetailLog(md);
    return false;
  }
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "Sys_Logs_Get",
        par: [
          { par: "Users_ID", va: opition.value.Users_ID },
          { par: "id", va: md.id },
        ],
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data);
      if (data.length > 0) {
        md = data[0][0];
        let mdx = datalists.value.find((x) => x.id == md.id);
        mdx.logcontent = md.logcontent;
        showDetailLog(md);
      }
      swal.close();
    })
    .catch((error) => {
      swal.close();
    });
};
const showDetailLog = (md) => {
  let obj = {};
  let erros = JSON.parse(md.logcontent);
  obj.contents = erros.contents;
  if (erros.data) {
    let arres = [];
    if (typeof erros.data == "string") {
      let objstr;
      try {
        objstr = JSON.parse(erros.data);
      } catch (error) {}
      if (objstr) {
        for (const [key, value] of Object.entries(objstr)) {
          if (key != "par") {
            arres.push({
              key: key,
              value: value,
              error: obj.contents && obj.contents.includes(`'${key}'`),
            });
          } else {
            value.forEach(function (d) {
              arres.push({
                key: d.par,
                value: d.va,
                error: obj.contents && obj.contents.includes(`'${d.par}'`),
              });
            });
          }
        }
      } else {
        arres.push({ key: "String", value: erros.data });
      }
    } else if (!(erros.data instanceof Object)) {
      JSON.parse(erros.data).forEach((element, i) => {
        arres.push({ key: i, value: JSON.stringify(element) });
      });
    } else {
      for (const [key, value] of Object.entries(erros.data)) {
        if (key != "par") {
          arres.push({
            key: key,
            value: value,
            error: obj.contents && obj.contents.includes(`'${key}'`),
          });
        } else {
          value.forEach(function (d) {
            arres.push({
              key: d.par,
              value: d.va,
              error: obj.contents && obj.contents.includes(`'${d.par}'`),
            });
          });
        }
      }
    }
    obj.data = arres;
  }
  obj.title = md.title;
  modelview.value = obj;
  displayAddData.value = true;
};
const closedisplayDetail = () => {
  displayAddData.value = false;
};
//Thêm sửa xoá
const onRefersh = () => {
  opition.value.search = "";
  opition.value = {
    IsNext: true,
    sort: "id DESC",
    PageNo: 0,
    PageSize: 20,
    FilterUsers_ID: null,
    Users_ID: store.getters.user.Users_ID,
  };
  isDynamicSQL.value = false;
  filterSQL.value = [];
  loadData(true);
};
const onSearch = () => {
  isDynamicSQL.value = false;
  opition.value.PageNo = 0;
  opition.value.id = null;
  opition.value.IsNext = true;
  opition.value.sort = "id DESC";
  loadData(true);
};
const addLog = (log) => {
  axios.post(baseURL + "/api/Proc/AddLog", log, config);
};
const initTudien = () => {
  axios
    .get(baseURL + "/api/Cache/ListUsers?cache=" + store.getters.Users_ID, {
      headers: { Authorization: `Bearer ${store.getters.token}` },
    })
    .then((response) => {
      let data = JSON.parse(response.data.data);
      if (data.length > 0) {
        tdUsers.value = data[0];
      }
    })
    .catch((error) => {});
};
const loadCount = () => {
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "Sys_Logs_Count",
        par: [
          { par: "Users_ID", va: opition.value.Users_ID },
          { par: "FilterUsers_ID", va: opition.value.FilterUsers_ID },
          { par: "PageNo", va: opition.value.PageNo },
          { par: "PageSize", va: opition.value.PageSize },
          { par: "Search", va: opition.value.search },
          { par: "IP", va: opition.value.IP },
          { par: "Loai", va: opition.value.Loai },
          { par: "module", va: opition.value.module },
          { par: "StartDate", va: opition.value.StartDate },
          { par: "EndDate", va: opition.value.EndDate },
        ],
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data)[0];
      if (data.length > 0) {
        opition.value.totalRecords = data[0].totalRecords;
      }
    })
    .catch((error) => {
      addLog({
        title: "Lỗi Console loadCount",
        controller: "LogsView.vue",
        logcontent: error.message,
        loai: 2,
      });
    });
};
const onPage = (event) => {
  if (event.page == 0) {
    //Trang đầu
    opition.value.id = null;
    opition.value.IsNext = true;
  } else if (event.page > opition.value.PageNo + 1) {
    //Trang cuối
    opition.value.id = -1;
    opition.value.IsNext = false;
  } else if (event.page > opition.value.PageNo) {
    //Trang sau
    opition.value.id = datalists.value[datalists.value.length - 1].id;
    opition.value.IsNext = true;
  } else if (event.page < opition.value.PageNo) {
    //Trang trước
    opition.value.id = datalists.value[0].id;
    opition.value.IsNext = false;
  }
  opition.value.PageNo = event.page;
  loadData(true);
};
const onSort = (event) => {
  opition.value.sort = event.sortField + (event.sortOrder == 1 ? " ASC" : " DESC");
  if (event.sortField != "id") {
    opition.value.sort += ",id " + (event.sortOrder == 1 ? " ASC" : " DESC");
  }
  isDynamicSQL.value = true;
  loadDataSQL();
};
const onFilter = (event) => {
  filterSQL.value = [];
  for (const [key, value] of Object.entries(event.filters)) {
    if (key != "global") {
      let obj = {
        key: key == "FullName" ? "Users_ID" : key,
        filteroperator: value.operator,
        filterconstraints: value.constraints,
      };
      if (value.value && value.value.length > 0) {
        obj.filteroperator = value.matchMode;
        obj.filterconstraints = [];
        value.value.forEach(function (vl) {
          obj.filterconstraints.push({ value: vl[obj.key] });
        });
      } else if (value.matchMode) {
        obj.filteroperator = "and";
        obj.filterconstraints = [value];
      }
      if (
        obj.filterconstraints &&
        obj.filterconstraints.filter((x) => x.value != null).length > 0
      )
        filterSQL.value.push(obj);
    }
  }
  opition.value.PageNo = 0;
  opition.value.id = null;
  isDynamicSQL.value = true;
  loadDataSQL();
};
const loadDataSQL = () => {
  let data = {
    id: opition.value.id,
    next: opition.value.IsNext,
    sqlO: opition.value.sort,
    Search: opition.value.search,
    PageNo: opition.value.PageNo,
    PageSize: opition.value.PageSize,
    fieldSQLS: filterSQL.value,
  };
  opition.value.loading = true;
  axios
    .post(baseURL + "/api/SQL/FilterSQLSys_Logs", data, config)
    .then((response) => {
      let dt = JSON.parse(response.data.data);
      let data = dt[0];
      if (data.length > 0) {
        data.forEach((element, i) => {
          element.STT = (opition.value.PageNo - 1) * opition.value.PageSize + i + 1;
          element.Ngay = moment(new Date(element.logdate)).format("DD/MM/YYYY HH:mm:ss");
        });
        datalists.value = data;
      } else {
        datalists.value = [];
      }
      if (isFirst.value) isFirst.value = false;
      opition.value.loading = false;
      //Show Count nếu có
      if (dt.length == 2) {
        opition.value.totalRecords = dt[1][0].totalRecords;
      }
    })
    .catch((error) => {
      opition.value.loading = false;
      toast.error("Tải dữ liệu không thành công!");
      addLog({
        title: "Lỗi Console loadData",
        controller: "LogsView.vue",
        logcontent: error.message,
        loai: 2,
      });
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
const loadData = (rf) => {
  if (isDynamicSQL.value) {
    loadDataSQL();
    return false;
  }
  opition.value.loading = true;
  if (rf) {
    if (opition.value.PageNo == 0) {
      loadCount();
    }
  }
  let proc = "Sys_Logs_ListSeek";
  let datas = [
    { par: "id", va: opition.value.id },
    { par: "IsNext", va: opition.value.IsNext },
    { par: "Users_ID", va: opition.value.Users_ID },
    { par: "FilterUsers_ID", va: opition.value.FilterUsers_ID },
    { par: "PageNo", va: opition.value.PageNo },
    { par: "PageSize", va: opition.value.PageSize },
    { par: "Search", va: opition.value.search },
    { par: "IP", va: opition.value.IP },
    { par: "Loai", va: opition.value.Loai },
    { par: "module", va: opition.value.module },
    { par: "StartDate", va: opition.value.StartDate },
    { par: "EndDate", va: opition.value.EndDate },
  ];
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: proc,
        par: datas,
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data)[0];
      if (data.length > 0) {
        data.forEach((element, i) => {
          element.STT = (opition.value.PageNo - 1) * opition.value.PageSize + i + 1;
          element.Ngay = moment(new Date(element.logdate)).format("DD/MM/YYYY HH:mm:ss");
        });
        datalists.value = data;
      } else {
        datalists.value = [];
      }
      if (isFirst.value) isFirst.value = false;
      opition.value.loading = false;
    })
    .catch((error) => {
      toast.error("Tải dữ liệu không thành công!");
      opition.value.loading = false;
      addLog({
        title: "Lỗi Console loadData",
        controller: "LogsView.vue",
        logcontent: error.message,
        loai: 2,
      });
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
        excelname: "LOGS",
        proc: "Sys_Logs_ListExport",
        par: [
          { par: "Users_ID", va: opition.value.Users_ID },
          { par: "FilterUsers_ID", va: opition.value.FilterUsers_ID },
          { par: "Search", va: opition.value.search },
          { par: "IP", va: opition.value.IP },
          { par: "Loai", va: opition.value.Loai },
          { par: "module", va: opition.value.module },
          { par: "StartDate", va: opition.value.StartDate },
          { par: "EndDate", va: opition.value.EndDate },
        ],
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
const rowClass = (data) => {
  return data.loai == 1 ? "success" : "error";
};
const rowClassError = (data) => {
  return data.error ? "error" : "success";
};

onMounted(() => {
  store.commit("setisadmin",true);
  //init
  initTudien();
  loadData(true);
  return {
    displayAddData,
    isFirst,
    opition,
    showModalDetail,
    closedisplayDetail,
    onSearch,
    basedomainURL,
    filters,
    onRefersh,
    itemButs,
    menuButs,
    toggleExport,
    onPage,
    modelview,
  };
});
</script>
<template>
  <div class="main-layout flex flex-column flex-grow-1 p-2" v-if="store.getters.islogin">
    <DataTable
      class="w-full p-datatable-sm e-sm"
      :lazy="true"
      @page="onPage($event)"
      @filter="onFilter($event)"
      @sort="onSort($event)"
      :value="datalists"
      :loading="opition.loading"
      :paginator="opition.totalRecords > opition.PageSize"
      :rows="opition.PageSize"
      :totalRecords="opition.totalRecords"
      dataKey="id"
      :rowHover="true"
      v-model:filters="filters"
      filterDisplay="menu"
      :showGridlines="true"
      filterMode="lenient"
      paginatorTemplate="FirstPageLink PrevPageLink CurrentPageReport NextPageLink LastPageLink"
      :rowsPerPageOptions="[10, 20, 50, 100]"
      :currentPageReportTemplate="
        isDynamicSQL ? '{currentPage}' : '{currentPage}/{totalPages}'
      "
      responsiveLayout="scroll"
      :scrollable="true"
      scrollHeight="flex"
      :rowClass="rowClass"
    >
      <template #header>
        <h3 class="module-title mt-0 ml-1 mb-2">
          <i class="pi pi-list"></i> Theo dõi Log
          <span v-if="opition.totalRecords > 0"
            >({{ opition.totalRecords.toLocaleString() }})</span
          >
        </h3>
        <div class="flex justify-content-center align-items-center">
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
                label="Export"
                icon="pi pi-file-excel"
                class="mr-2 p-button-outlined p-button-secondary"
                @click="toggleExport"
                aria-haspopup="true"
                aria-controls="overlay_Export"
              />
              <Menu id="overlay_Export" ref="menuButs" :model="itemButs" :popup="true" />
            </template>
          </Toolbar>
        </div>
      </template>
      <Column
        field="id"
        header="ID"
        :sortable="true"
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:120px"
        bodyStyle="text-align:center;max-width:120px"
      ></Column>
      <Column field="title" header="Tiêu đề" class="align-items-center">
        <template #body="md">
          <InlineMessage
            style="justify-content: center"
            v-bind:severity="md.data.loai == 1 ? 'success' : 'error'"
            >{{ md.data.title }}</InlineMessage
          >
        </template>
        <template #filter="{ filterModel }">
          <InputText
            type="text"
            v-model="filterModel.value"
            class="p-column-filter"
            placeholder="Từ khoá"
          />
        </template>
      </Column>
      <Column
        :sortable="true"
        field="Users_ID"
        header="Tài khoản"
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:150px"
        bodyStyle="text-align:center;max-width:150px"
      >
        <template #body="{ data }">
          {{ data.Users_ID }}
        </template>
        <template #filter="{ filterModel }">
          <InputText
            type="text"
            v-model="filterModel.value"
            class="p-column-filter"
            placeholder="Từ khoá"
          />
        </template>
      </Column>
      <Column
        :sortable="true"
        field="FullName"
        filterField="FullName"
        header="Tên người dùng"
        :showFilterMatchModes="false"
      >
        <template #body="{ data }">
          <span class="image-text">{{ data.FullName }}</span>
        </template>
        <template #filter="{ filterModel }">
          <MultiSelect
            v-model="filterModel.value"
            :options="tdUsers"
            optionLabel="FullName"
            placeholder="Chọn user"
            class="p-column-filter"
          >
            <template #option="slotProps">
              <div class="p-multiselect-representative-option">
                <Avatar
                  v-bind:label="
                    slotProps.option.Avartar
                      ? ''
                      : slotProps.option.FullName.substring(0, 1)
                  "
                  v-bind:image="basedomainURL + slotProps.option.Avartar"
                  style="
                    background-color: #2196f3;
                    color: #ffffff;
                    vertical-align: middle;
                  "
                  class="mr-2"
                  size="small"
                  shape="circle"
                />
                <span class="image-text">{{ slotProps.option.FullName }}</span>
              </div>
            </template>
          </MultiSelect>
        </template>
      </Column>
      <Column
        :sortable="true"
        field="logdate"
        dataType="date"
        header="Ngày truy cập"
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:200px"
        bodyStyle="text-align:center;max-width:200px"
      >
        <template #body="{ data }">
          {{ data.Ngay }}
        </template>
        <template #filter="{ filterModel }">
          <Calendar v-model="filterModel.value" />
        </template>
      </Column>
      <Column
        :sortable="true"
        field="IP"
        header="IP"
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:120px"
        bodyStyle="text-align:center;max-width:120px"
      >
        <template #body="{ data }">
          {{ data.IP }}
        </template>
        <template #filter="{ filterModel }">
          <InputText
            type="text"
            v-model="filterModel.value"
            class="p-column-filter"
            placeholder="Từ khoá"
          />
        </template>
      </Column>
      <Column
        field="loai"
        headerStyle="max-width: 60px"
        headerClass="text-center"
        bodyClass="text-center"
        :showFilterMatchModes="false"
        bodyStyle="text-align:center;max-width:60px"
      >
        <template #header> </template>
        <template #body="md">
          <Button
            v-if="md.data.loai != 1"
            type="button"
            icon="pi pi-info-circle"
            class="p-button-sm p-button-secondary"
            @click="showModalDetail(md.data)"
          ></Button>
        </template>
        <template #filter="{ filterModel }">
          <Dropdown
            v-model="filterModel.value"
            :options="tdLoais"
            optionLabel="name"
            optionValue="id"
            placeholder="Chọn loại"
            class="p-column-filter"
            :showClear="true"
          >
          </Dropdown>
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
  </div>
  <Dialog
    header="Chi tiết"
    v-model:visible="displayAddData"
    :style="{ width: '70vw', zIndex: 1000 }"
    :maximizable="true"
    :autoZIndex="false"
    :modal="true"
  >
    <div class="grid">
      <div class="col-4 h-full" v-if="modelview.data">
        <Panel header="Tham số truyền vào">
          <div v-if="modelview.data.length == 1">
            <h3>{{ modelview.data[0].key }}</h3>
            <h5 style="word-break: break-word">{{ modelview.data[0].value }}</h5>
          </div>
          <DataTable
            class="p-datatable-sm"
            showGridlines
            v-if="modelview.data.length > 1"
            :value="modelview.data"
            :rowClass="rowClassError"
            responsiveLayout="scroll"
            :scrollable="true"
          >
            <Column
              field="key"
              header="Tham số"
              headerStyle="background-color:aliceblue;font-weight:bold;max-width:120px"
              bodyStyle="max-width:120px"
            ></Column>
            <Column
              field="value"
              header="Giá trị"
              headerStyle="background-color:aliceblue;font-weight:bold"
              bodyStyle="word-break:break-word"
            ></Column>
          </DataTable>
        </Panel>
      </div>
      <div :class="'col-' + (modelview.data ? 8 : 12)">
        <Panel header="Chi tiết lỗi">
          <InlineMessage style="width: 100%; justify-content: start">{{
            modelview.title
          }}</InlineMessage>
          <div
            style="
              background: rgba(0, 0, 0, 0.8);
              color: #fff;
              padding: 10px;
              overflow: auto;
            "
          >
            <code id="diverrrorContent" class="m-3" v-html="modelview.contents"></code>
          </div>
        </Panel>
      </div>
    </div>

    <template #footer>
      <Button label="Đóng lại" icon="pi pi-times" @click="closedisplayDetail" />
    </template>
  </Dialog>
</template>
<style scoped>
.boxtable {
  height: calc(100% - 60px);
}
</style>
