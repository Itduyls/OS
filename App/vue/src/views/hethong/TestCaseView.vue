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
  NgayTest: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }],
  },
  IP: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.STARTS_WITH }],
  },
  IsPass: { value: null, matchMode: FilterMatchMode.EQUALS },
  FullName: { value: null, matchMode: FilterMatchMode.IN },
  Case_Name: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.STARTS_WITH }],
  },
  SoTest: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }],
  },
});
const tdLoais = ref([
  { id: true, name: "Thành công" },
  { id: false, name: "Thất bại" },
]);
const tdDuans = ref([]);
const tdCongviecs = ref([]);
const modelview = ref({});
const datalists = ref();
const tdUsers = ref([]);
const displayDetailData = ref(false);
const isFirst = ref(true);
const filterSQL = ref([]);
const isDynamicSQL = ref(true); //phân trang bình thường hay phân trang tối ưu cho dữ liệu lớn
const toast = useToast();
const swal = inject("$swal");
const store = inject("store");
const axios = inject("axios"); // inject axios
const socket = inject("socket");
//Chờ socket
socket.on("python", (data) => {
  if (data.error) {
    console.log(data);
    testcase.value.start = false;
    var tc = datalists.value.find((x) => x.Case_ID == testcase.value.Case_ID);
    tc.start = false;
    toast.error("Chạy Test không thành công! Vui lòng thử lại.");
    return false;
  }
  testcase.value.start = false;
  toast.success("Chạy Test thành công");
  loadData(true);
});
const opition = ref({
  IsNext: true,
  sort: "Case_ID DESC",
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
  Case_Name.value = md.Case_Name;
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
        proc: "Test_Case_Info",
        par: [
          { par: "Users_ID", va: opition.value.Users_ID },
          { par: "Case_ID", va: md.Case_ID },
        ],
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data);
      if (data.length > 0) {
        md = data[0];
        md.forEach((element, i) => {
          element.STT = md.length - i;
          element.NgayBatdau = moment(new Date(element.Batdau)).format(
            "DD/MM/YYYY HH:mm:ss"
          );
          element.NgayKetthuc = moment(new Date(element.Ketthuc)).format(
            "DD/MM/YYYY HH:mm:ss"
          );
        });
        goLanTest(md[0]);
        showDetailLog(md);
      }
      swal.close();
    })
    .catch((error) => {
      swal.close();
    });
};
const Lan_ID = ref();
const Case_Name = ref("Chi tiết");
const goLanTest = (md) => {
  Lan_ID.value = md.Lan_ID;
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
        proc: "Test_Step_Info",
        par: [
          { par: "Users_ID", va: opition.value.Users_ID },
          { par: "Lan_ID", va: md.Lan_ID },
        ],
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data);
      if (data.length > 0) {
        md = data[0];
        md.forEach((element, i) => {
          element.NgayBatdau = moment(new Date(element.Batdau)).format(
            "DD/MM/YYYY HH:mm:ss"
          );
          element.NgayKetthuc = moment(new Date(element.Ketthuc)).format(
            "DD/MM/YYYY HH:mm:ss"
          );
        });
        stepTest.value = md;
      }
      swal.close();
    })
    .catch((error) => {
      swal.close();
    });
};
const lanTest = ref([]);
const stepTest = ref([]);
const showDetailLog = (md) => {
  displayDetailData.value = true;
  lanTest.value = md;
};
const closedisplayDetail = () => {
  displayDetailData.value = false;
};
//Thêm sửa xoá
const onRefersh = () => {
  opition.value.search = "";
  opition.value = {
    IsNext: true,
    sort: "Case_ID DESC",
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
  opition.value.sort = "Case_ID DESC";
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
        proc: "Test_Case_Count",
        par: [
          { par: "Users_ID", va: opition.value.Users_ID },
          { par: "FilterUsers_ID", va: opition.value.FilterUsers_ID },
          { par: "PageNo", va: opition.value.PageNo },
          { par: "PageSize", va: opition.value.PageSize },
          { par: "Search", va: opition.value.search },
          { par: "IP", va: opition.value.IP },
          { par: "IsPass", va: opition.value.IsPass },
          { par: "Duan_ID", va: opition.value.Duan_ID },
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
  if (event.sortField != "Case_ID") {
    opition.value.sort += ",Case_ID " + (event.sortOrder == 1 ? " ASC" : " DESC");
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
    .post(baseURL + "/api/SQL/FilterSQLTest_Case", data, config)
    .then((response) => {
      let dt = JSON.parse(response.data.data);
      let data = dt[0];
      if (data.length > 0) {
        data.forEach((element, i) => {
          element.STT = (opition.value.PageNo - 1) * opition.value.PageSize + i + 1;
          element.Ngay = moment(new Date(element.NgayTest)).format("DD/MM/YYYY HH:mm:ss");
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
  let proc = "Test_Case_ListSeek";
  let datas = [
    { par: "Case_ID", va: opition.value.id },
    { par: "IsNext", va: opition.value.IsNext },
    { par: "Users_ID", va: opition.value.Users_ID },
    { par: "FilterUsers_ID", va: opition.value.FilterUsers_ID },
    { par: "PageNo", va: opition.value.PageNo },
    { par: "PageSize", va: opition.value.PageSize },
    { par: "Search", va: opition.value.search },
    { par: "IP", va: opition.value.IP },
    { par: "IsPass", va: opition.value.IsPass },
    { par: "Duan_ID", va: opition.value.Duan_ID },
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
          element.Ngay = moment(new Date(element.NgayTest)).format("DD/MM/YYYY HH:mm:ss");
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
        proc: "Test_Case_ListExport",
        par: [
          { par: "Users_ID", va: opition.value.Users_ID },
          { par: "FilterUsers_ID", va: opition.value.FilterUsers_ID },
          { par: "Search", va: opition.value.search },
          { par: "IP", va: opition.value.IP },
          { par: "IsPass", va: opition.value.IsPass },
          { par: "Duan_ID", va: opition.value.Duan_ID },
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
  if (data.SoTest < 1) return "";
  return data.IsPass ? "success" : "error";
};
const rowClassError = (data) => {
  return data.IsPass != true ? "error" : "success";
};
const testcase = ref({});
const displayAddCase = ref(false);
const isAdd = ref(true);
const submitted = ref(true);
const showModalAddTestCase = () => {
  isAdd.value = true;
  submitted.value = false;
  testcase.value = {
    Case_Name: "",
    STT: datalists.value.length + 1,
    Trangthai: true,
    IsUrl: "http://localhost:3000",
    ShowChrome: true,
  };
  displayAddCase.value = true;
};
const closedisplayAddCase = () => {
  displayAddCase.value = false;
};
const addCase = () => {
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  let md = { ...testcase.value };
  if (md.Tukhoa instanceof Array) {
    md.Tukhoa = md.Tukhoa.join(",");
  }
  if (md.ShowChrome) {
    md.CodeTest = md.CodeTest.replace(
      "options.headless = True",
      "options.headless = False"
    );
  } else {
    md.CodeTest = md.CodeTest.replace(
      "options.headless = False",
      "options.headless = True"
    );
  }
  axios({
    method: isAdd.value == false ? "put" : "post",
    url: baseURL + `/api/TestCase/${isAdd.value == false ? "Update_Case" : "Add_Case"}`,
    data: md,
    headers: {
      Authorization: `Bearer ${store.getters.token}`,
    },
  })
    .then((response) => {
      if (response.data.err != "1") {
        updatePython = true;
        swal.close();
        toast.success("Cập nhật Test Case thành công!");
        loadData(true);
        closedisplayAddCase();
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
let updatePython = false;
const startCase = (md) => {
  md.start = true;
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "Test_Case_Get",
        par: [
          { par: "Users_ID", va: store.getters.user.Users_ID },
          { par: "Case_ID", va: md.Case_ID },
        ],
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data);
      if (data.length > 0) {
        let obj = data[0][0];
        if (obj.Tukhoa) obj.Tukhoa = obj.Tukhoa.split(",");
        testcase.value = obj;
        socket.emit("pythonTest", {
          FileTest: obj.FileTest,
          CodeTest: obj.CodeTest,
          update: updatePython,
        });
        updatePython = false;
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
const editCase = (md) => {
  isAdd.value = false;
  submitted.value = false;
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  displayAddCase.value = true;
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "Test_Case_Get",
        par: [
          { par: "Users_ID", va: store.getters.user.Users_ID },
          { par: "Case_ID", va: md.Case_ID },
        ],
      },
      config
    )
    .then((response) => {
      swal.close();
      let data = JSON.parse(response.data.data);
      if (data.length > 0) {
        let obj = data[0][0];
        if (obj.Tukhoa) obj.Tukhoa = obj.Tukhoa.split(",");
        testcase.value = obj;
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
const delCase = (md) => {
  swal
    .fire({
      title: "Thông báo",
      text: "Bạn có muốn xoá test case này không!",
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
          .delete(baseURL + "/api/TestCase/Del_Case", {
            headers: { Authorization: `Bearer ${store.getters.token}` },
            data: [md.Case_ID],
          })
          .then((response) => {
            swal.close();
            if (response.data.err != "1") {
              swal.close();
              toast.success("Xoá Case thành công!");
              loadData(true);
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
const initTudienDuan = () => {
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "DuanCongviec_ListTudien",
        par: [{ par: "Users_ID", va: store.getters.user.Users_ID }],
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data);
      if (data.length > 0) {
        tdDuans.value = data[0];
        tdCongviecs.value = data[1];
      }
    })
    .catch((error) => {});
};
onMounted(() => {
  //init
  initTudienDuan();
  initTudien();
  loadData(true);
  return {
    displayDetailData,
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
      dataKey="Case_ID"
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
          <i class="pi pi-list"></i> Theo dõi Test Case
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
              <Button
                label="Thêm Test Case"
                icon="pi pi-plus"
                class="mr-2"
                @click="showModalAddTestCase"
              />
            </template>
          </Toolbar>
        </div>
      </template>
      <Column
        field="Case_ID"
        header="Case_ID"
        :sortable="true"
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:120px"
        bodyStyle="text-align:center;max-width:120px"
      ></Column>
      <Column field="Case_Name" header="Tên chức năng Test" class="align-items-center">
        <template #body="md">
          <InlineMessage
            style="justify-content: center"
            v-bind:severity="
              md.data.SoTest < 1 ? '' : md.data.IsPass ? 'success' : 'error'
            "
            >{{ md.data.Case_Name }}</InlineMessage
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
        field="NgayTest"
        dataType="date"
        header="Ngày Test"
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
        field="SoTest"
        dataType="numeric"
        header="Lần Test"
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:140px"
        bodyStyle="text-align:center;max-width:140px"
      >
        <template #body="{ data }">
          <Badge
            @click="showModalDetail(data)"
            size="large"
            v-bind:value="data.SoTest"
            v-bind:severity="
              data.SoTest <= 2
                ? 'success'
                : data.SoTest <= 5
                ? 'info'
                : data.SoTest <= 10
                ? 'warning'
                : 'danger'
            "
          ></Badge>
        </template>
        <template #filter="{ filterModel }">
          <InputNumber v-model="filterModel.value" />
        </template>
      </Column>

      <Column
        :sortable="true"
        field="IPtao"
        header="IP"
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:120px"
        bodyStyle="text-align:center;max-width:120px"
      >
        <template #body="{ data }">
          {{ data.IPtao }}
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
        field="IsPass"
        headerStyle="max-width: 60px"
        headerClass="text-center"
        bodyClass="text-center"
        :showFilterMatchModes="false"
        bodyStyle="text-align:center;max-width:60px"
      >
        <template #header> </template>
        <template #body="md">
          <Button
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
            v-bind:icon="
              'pi ' + (md.data.start == true ? 'pi-spin pi-spinner' : 'pi-step-forward')
            "
            class="p-button-rounded p-button-sm p-button-success"
            style="margin-right: 0.5rem"
            v-tooltip="'Chạy Test'"
            @click="startCase(md.data)"
          ></Button>
          <Button
            type="button"
            icon="pi pi-pencil"
            class="p-button-rounded p-button-sm p-button-info"
            style="margin-right: 0.5rem"
            @click="editCase(md.data)"
          ></Button>
          <Button
            type="button"
            icon="pi pi-trash"
            class="p-button-rounded p-button-sm p-button-danger"
            @click="delCase(md.data)"
          ></Button>
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
    header="Cập nhật Test Case"
    v-model:visible="displayAddCase"
    :style="{ width: '720px', zIndex: 1000 }"
    :maximizable="true"
    :autoZIndex="false"
    :modal="true"
  >
    <form @submit.prevent="addTestCase">
      <div class="grid formgrid m-2">
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left">Mã Case <span class="redsao">(*)</span></label>
          <InputText
            spellcheck="false"
            disabled="true"
            class="col-4 ip36"
            v-model="testcase.Case_ID"
          />
          <label class="col-2 text-left">Tên File Case</label>
          <InputText spellcheck="false" class="col-4 ip36" v-model="testcase.FileTest" />
        </div>
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left">Tên Case <span class="redsao">(*)</span></label>
          <InputText
            spellcheck="false"
            class="col-10 ip36"
            v-model="testcase.Case_Name"
          />
        </div>
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left">Dự án</label>
          <Dropdown
            class="col-4"
            v-model="testcase.Duan_ID"
            :options="tdDuans"
            optionLabel="Duan_Ten"
            optionValue="Duan_ID"
            placeholder="Chọn dự án"
          >
          </Dropdown>

          <label class="col-2 text-right">Công việc</label>
          <Dropdown
            class="col-4"
            v-model="testcase.Congviec_ID"
            :options="tdCongviecs"
            optionLabel="Congviec_Ten"
            optionValue="Congviec_ID"
            placeholder="Chọn công việc"
          >
          </Dropdown>
        </div>
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left">Từ khoá</label>
          <Chips
            spellcheck="false"
            class="col-10 p-0"
            v-model="testcase.Tukhoa"
            :addOnBlur="true"
            separator=","
          />
        </div>
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left">STT</label>
          <InputNumber class="col-2 ip36 p-0" v-model="testcase.STT" />
          <label class="col-2 text-left">Trạng thái</label>
          <InputSwitch
            v-model="testcase.Trangthai"
            class="mt-1"
            style="vertical-align: bottom"
          />
          <label class="col-3 text-right">Show trình duyệt</label>
          <InputSwitch
            v-model="testcase.ShowChrome"
            class="mt-1"
            style="vertical-align: bottom"
          />
        </div>
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left">Code Test</label>
          <Textarea
            v-model="testcase.CodeTest"
            spellcheck="false"
            rows="10"
            class="w-full"
            style="background-color: rgba(0, 0, 0, 0.8); color: #fff"
          ></Textarea>
        </div>
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left">Địa chỉ Test</label>
          <InputText spellcheck="false" class="col-10 ip36" v-model="testcase.IsUrl" />
        </div>
      </div>
    </form>
    <template #footer>
      <Button
        label="Huỷ"
        icon="pi pi-times"
        @click="closedisplayAddCase"
        class="p-button-raised p-button-secondary"
      />
      <Button label="Cập nhật" icon="pi pi-save" @click="addCase" />
    </template>
  </Dialog>
  <Dialog
    :header="Case_Name"
    v-model:visible="displayDetailData"
    :style="{ width: '70vw', zIndex: 1000 }"
    :maximizable="true"
    :autoZIndex="false"
    :modal="true"
  >
    <div class="grid" style="min-height: 70vh">
      <div class="col-3 h-full">
        <Panel header="Lần Test">
          <ScrollPanel style="width: 100%; height: calc(100vh - 375px)">
            <div
              v-for="item in lanTest"
              v-bind:key="item.Lan_ID"
              @click="goLanTest(item)"
              v-bind:class="
                'mt-2 text-center card p-ripple IsPass' +
                item.IsPass +
                (item.Lan_ID == Lan_ID ? ' active' : '')
              "
              class="card primary-box p-ripple"
              v-ripple
            >
              Lần {{ item.STT }}<br />
              ({{ item.NgayBatdau }})
            </div>
          </ScrollPanel>
        </Panel>
      </div>
      <div class="col-9 h-full">
        <Panel header="Chi tiết bước">
          <DataTable
            showGridlines
            :value="stepTest"
            :rowClass="rowClassError"
            responsiveLayout="scroll"
            :scrollable="true"
          >
            <Column
              field="STT"
              header="STT"
              class="align-items-center justify-content-center text-center"
              headerStyle="background-color:aliceblue;font-weight:bold;max-width:80px"
              bodyStyle="word-break:break-word;padding:5px;max-width:80px"
            >
              <template #body="md">
                <Badge
                  class="mt-2 mb-2"
                  :severity="md.data.IsPass ? 'info' : 'danger'"
                  v-bind:value="md.data.STT"
                ></Badge>
              </template>
            </Column>
            <Column
              field="Step_Name"
              header="Tên bước"
              headerStyle="background-color:aliceblue;font-weight:bold"
              bodyStyle="word-break:break-word;padding:5px"
            >
              <template #body="md">
                <div>
                  <div class="p-2 block" style="font-weight: 500">
                    {{ md.data.Step_Name }}
                  </div>
                  <div class="block ml-2">
                    <i>{{ md.data.Mota }}</i>
                  </div>
                </div>
              </template>
            </Column>
            <Column
              field="Hinhanh"
              header="Hình ảnh"
              class="align-items-center justify-content-center text-center"
              headerStyle="background-color:aliceblue;font-weight:bold;max-width:120px"
              bodyStyle="word-break:break-word;padding:5px;max-width:120px"
            >
              <template #body="md">
                <Image
                  v-bind:src="basedomainURL + md.data.Hinhanh"
                  alt=""
                  height="48"
                  preview
                />
              </template>
            </Column>
          </DataTable>
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
.card {
  cursor: pointer;
}
.card.IsPasstrue {
  background-color: #65c412;
  color: #fff;
  font-size: 13pt;
  font-weight: 600;
  padding: 10px;
}
.card.IsPassfalse {
  background-color: #f44336;
  color: #fff;
  font-size: 13pt;
  font-weight: 600;
  padding: 10px;
}
.card.active,
.card:hover {
  background-color: #0b7ad1;
}
</style>
