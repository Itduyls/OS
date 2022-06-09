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
  IsTime: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }],
  },
  FromIP: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.STARTS_WITH }],
  },
  FromDivice: {
    operator: FilterOperator.OR,
    constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }],
  },
  FullName: { value: null, matchMode: FilterMatchMode.IN },
});
const tdFromDivice = ref(["PC", "Android", "IOS", "Laptop"]);
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
  sort: "WebAcess_ID DESC",
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
const showModalDetail = () => {
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
    sort: "WebAcess_ID DESC",
    PageNo: 0,
    IsLess: true,
    PageSize: 20,
    FilterUsers_ID: null,
    Users_ID: store.getters.user.Users_ID,
  };
  //isDynamicSQL.value = false;
  filterSQL.value = [];
  loadData(true);
};
const onSearch = () => {
  //isDynamicSQL.value = false;
  opition.value.PageNo = 0;
  opition.value.WebAcess_ID = null;
  opition.value.IsNext = true;
  opition.value.sort = "WebAcess_ID DESC";
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
        proc: "Sys_WebAcess_Count",
        par: [
          { par: "Search", va: opition.value.search },
          { par: "Users_ID", va: opition.value.Users_ID },
          { par: "FilterUsers_ID", va: opition.value.FilterUsers_ID },
          { par: "PageNo", va: opition.value.PageNo },
          { par: "PageSize", va: opition.value.PageSize },
          { par: "FromDivice", va: opition.value.FromDivice },
          { par: "IP", va: opition.value.IP },
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
        controller: "WebAcessView.vue",
        logcontent: error.message,
        loai: 2,
      });
    });
};
const onPage = (event) => {
  if (event.page == 0) {
    //Trang đầu
    opition.value.WebAcess_ID = null;
    opition.value.IsNext = true;
  } else if (event.page > opition.value.PageNo + 1) {
    //Trang cuối
    opition.value.WebAcess_ID = -1;
    opition.value.IsNext = false;
  } else if (event.page > opition.value.PageNo) {
    //Trang sau
    opition.value.WebAcess_ID = datalists.value[datalists.value.length - 1].WebAcess_ID;
    opition.value.IsNext = true;
  } else if (event.page < opition.value.PageNo) {
    //Trang trước
    opition.value.WebAcess_ID = datalists.value[0].WebAcess_ID;
    opition.value.IsNext = false;
  }
  opition.value.PageNo = event.page;
  loadData(true);
};
const onSort = (event) => {
  opition.value.sort = event.sortField + (event.sortOrder == 1 ? " ASC" : " DESC");
  if (event.sortField != "WebAcess_ID") {
    opition.value.sort += ",WebAcess_ID " + (event.sortOrder == 1 ? " ASC" : " DESC");
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
  opition.value.WebAcess_ID = null;
  isDynamicSQL.value = true;
  loadDataSQL();
};
const loadDataSQL = () => {
  let data = {
    id: opition.value.WebAcess_ID,
    next: opition.value.IsNext,
    sqlO: opition.value.sort,
    Search: opition.value.search,
    PageNo: opition.value.PageNo,
    PageSize: opition.value.PageSize,
    fieldSQLS: filterSQL.value,
  };
  opition.value.loading = true;
  axios
    .post(baseURL + "/api/SQL/FilterSQLSys_WebAcess", data, config)
    .then((response) => {
      let dt = JSON.parse(response.data.data);
      let data = dt[0];
      if (data.length > 0) {
        data.forEach((element, i) => {
          element.STT = (opition.value.PageNo - 1) * opition.value.PageSize + i + 1;
          element.Ngay = moment(new Date(element.IsTime)).format("DD/MM/YYYY HH:mm:ss");
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
        controller: "WebAcessView.vue",
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
  let proc = "Sys_WebAcess_ListSeek";
  let datas = [
    { par: "WebAcess_ID", va: opition.value.WebAcess_ID },
    { par: "IsNext", va: opition.value.IsNext },
    { par: "Search", va: opition.value.search },
    { par: "Users_ID", va: opition.value.Users_ID },
    { par: "FilterUsers_ID", va: opition.value.FilterUsers_ID },
    { par: "PageNo", va: opition.value.PageNo },
    { par: "PageSize", va: opition.value.PageSize },
    { par: "FromDivice", va: opition.value.FromDivice },
    { par: "IP", va: opition.value.IP },
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
          element.Ngay = moment(new Date(element.IsTime)).format("DD/MM/YYYY HH:mm:ss");
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
        controller: "WebAcessView.vue",
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
        excelname: "LỊCH SỬ TRUY CẬP",
        proc: "Sys_WebAcess_ListExport",
        par: [
          { par: "Search", va: opition.value.search },
          { par: "Users_ID", va: opition.value.Users_ID },
          { par: "FilterUsers_ID", va: opition.value.FilterUsers_ID },
          { par: "FromDivice", va: opition.value.FromDivice },
          { par: "IP", va: opition.value.IP },
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
        store.commit("gologout");
      }
    });
};

onMounted(() => {
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
    onFilter,
    onSort,
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
      dataKey="WebAcess_ID"
      :rowHover="true"
      v-model:filters="filters"
      filterDisplay="menu"
      :showGridlines="true"
      filterMode="lenient"
      :pageLinkSize="opition.PageSize"
      paginatorTemplate="FirstPageLink PrevPageLink CurrentPageReport NextPageLink LastPageLink"
      :rowsPerPageOptions="[10, 20, 50, 100]"
      :currentPageReportTemplate="
        isDynamicSQL ? '{currentPage}' : '{currentPage}/{totalPages}'
      "
      responsiveLayout="scroll"
      :scrollable="true"
      scrollHeight="flex"
    >
      <template #header>
        <h3 class="module-title mt-0 ml-1 mb-2">
          <i class="pi pi-database"></i> Lịch sử truy cập
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
        :sortable="true"
        field="WebAcess_ID"
        header="ID"
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:120px"
        bodyStyle="text-align:center;max-width:120px"
      ></Column>
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
        field="IsTime"
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
        field="FromIP"
        header="IP"
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:100px"
        bodyStyle="text-align:center;max-width:100px"
      >
        <template #body="{ data }">
          {{ data.FromIP }}
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
        field="FromDivice"
        header="Thiết bị"
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:140px"
        bodyStyle="text-align:center;max-width:140px"
      >
        <template #body="{ data }">
          <span>{{ data.FromDivice }}</span>
        </template>
        <template #filter="{ filterModel }">
          <Dropdown
            v-model="filterModel.value"
            :options="tdFromDivice"
            placeholder="Chọn thiết bị"
            class="p-column-filter"
            :showClear="true"
          >
            <template #value="slotProps">
              <span
                :class="'customer-badge status-' + slotProps.value"
                v-if="slotProps.value"
                >{{ slotProps.value }}</span
              >
              <span v-else>{{ slotProps.placeholder }}</span>
            </template>
            <template #option="slotProps">
              <span>{{ slotProps.option }}</span>
            </template>
          </Dropdown>
        </template>
      </Column>
      <template #empty>
        <div
          class="align-items-center justify-content-center p-4 text-center w-full"
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
    :style="{ width: '480px', zIndex: 1000 }"
    :maximizable="true"
    :autoZIndex="false"
    :modal="true"
  >
    <template #footer>
      <Button label="Đóng lại" icon="pi pi-times" @click="closedisplayDetail" />
    </template>
  </Dialog>
</template>
<style scoped></style>
