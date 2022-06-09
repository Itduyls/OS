<script setup>
import { ref, inject, onMounted, watch } from "vue";
import { useToast } from "vue-toastification";
import { required } from "@vuelidate/validators";
import { useVuelidate } from "@vuelidate/core";
import { FilterMatchMode, FilterOperator } from "primevue/api";
import moment from "moment";
const axios = inject("axios");
const store = inject("store");
const swal = inject("$swal");
const isDynamicSQL = ref(false);
const config = {
  headers: { Authorization: `Bearer ${store.getters.token}` },
};
const filters = ref({
  global: { value: null, matchMode: FilterMatchMode.CONTAINS },
  dispatch_book_name: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.STARTS_WITH }],
  },
});
const rules = {
  dispatch_book_name: {
    required,
    $errors: [
      {
        $property: "dispatch_book_name",
        $validator: "required",
        $message: "Tên sổ công văn không được để trống!",
      },
    ],
  },
};
const dispatch = ref({
  dispatch_book_name: "",
  year: 1970,
  current_num: 1,
  tracking_place: "",
  nav_type: 0,
  status: true,
  is_order: 1,
});
const selectedDispatchs = ref();
const submitted = ref(false);
const v$ = useVuelidate(rules, dispatch);
const issaveDispatch = ref(false);
const datalists = ref();
const toast = useToast();
const basedomainURL = baseURL;
const checkDelList = ref(false);
const departmentOptions = ref([
  { name: "Chọn phòng ban...", code: null },
  { name: "Kế toán", code: 0 },
  { name: "Nhân sự", code: 1 },
  { name: "Giám đốc", code: -1 },
]);
const typeOptions = ref([
  { name: "Chọn loại văn bản...", code: null },
  { name: "Văn bản đến", code: 1 },
  { name: "Văn bản đi", code: 2 },
  { name: "Nội bộ", code: 3 },
]);
const options = ref({
  IsNext: true,
  sort: "dispatch_book_id",
  SearchText: "",
  PageNo: 0,
  PageSize: 20,
  loading: true,
  totalRecords: null,
});
//Thêm log
const addLog = (log) => {
  axios.post(baseURL + "/api/Proc/AddLog", log, config);
};
//Lấy số bản ghi
const loadCount = () => {
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "ca_dispatch_book_count",
        par: [{ par: "search", va: options.value.SearchText }],
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data)[0];
      if (data.length > 0) {
        options.value.totalRecords = data[0].totalRecords;
        sttDispatch.value = options.value.totalRecords + 1;
      }
    })
    .catch((error) => {
      addLog({
        title: "Lỗi Console loadCount",
        controller: "SQLView.vue",
        logcontent: error.message,
        loai: 2,
      });
    });
};
const minDate = ref();
const loadData = (rf) => {
  if (rf) {
    if (isDynamicSQL.value) {
      loadDataSQL();
      return false;
    }
    if (rf) {
      if (options.value.PageNo == 0) {
        loadCount();
      }
    }

    axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "ca_dispatch_book_list",
          par: [
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
        if (isFirst.value) isFirst.value = false;
        data.forEach((element, i) => {
          element.is_order =
            options.value.PageNo * options.value.PageSize + i + 1;
          element.open_date = moment(new Date(element.open_date)).format(
            "DD/MM/YYYY"
          );
          element.end_date = moment(new Date(element.end_date)).format(
            "DD/MM/YYYY"
          );
        });
        datalists.value = data;
        options.value.loading = false;
      })
      .catch((error) => {
        toast.error("Tải dữ liệu không thành công!");
        options.value.loading = false;
        addLog({
          title: "Lỗi Console loadData",
          controller: "DispatchView.vue",
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
  }
};
//Sort
const onSort = (event) => {
  options.value.sort =
    event.sortField + (event.sortOrder == 1 ? " ASC" : " DESC");
  if (event.sortField != "dispatch_book_id") {
    options.value.sort +=
      ",dispatch_book_id " + (event.sortOrder == 1 ? " ASC" : " DESC");
  }
  options.value.PageNo = 1;
  checkSort.value = true;
  isDynamicSQL.value = true;
  loadDataSQL();
};
const checkSort = ref(false);
//Phân trang dữ liệu
const onPage = (event) => {
  options.value.id = null;
  options.value.IsNext = true;
  options.value.PageNo = event.page + 1;

  loadDataSQL();
};
//Hiển thị dialog
const headerDialog = ref();
const displayBasic = ref(false);
const openBasic = (str) => {
  submitted.value = false;
  dispatch.value = {
    dispatch_book_name: "",
    is_order: sttDispatch.value,
    status: true,
  };
  if (store.state.user.IsAdmin) {
    dispatch.value.organization_id = 0;
  }
  issaveDispatch.value = false;
  headerDialog.value = str;
  displayBasic.value = true;
};
const closeDialog = () => {
  dispatch.value = {
    dispatch_book_name: "",
    is_order: 1,
    status: true,
  };
  displayBasic.value = false;
};

//Thêm bản ghi
const saveDispatch = (isFormValid) => {
  submitted.value = true;
  if (!isFormValid) {
    return;
  }
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  if (issaveDispatch.value) {
    if (typeof dispatch.value.open_date == "string") {
      let arr = dispatch.value.open_date.split("/");
      dispatch.value.open_date = new Date(arr[1] + "/" + arr[0] + "/" + arr[2]);
    }
    if (typeof dispatch.value.end_date == "string") {
      let arr1 = dispatch.value.end_date.split("/");
      dispatch.value.end_date = new Date(
        arr1[1] + "/" + arr1[0] + "/" + arr1[2]
      );
    }
  }
  axios({
    method: issaveDispatch.value ? "put" : "post",
    url:
      baseURL +
      `/api/ca_dispatch_books/${
        issaveDispatch.value ? "Update_dispatch_books" : "Add_dispatch_book"
      }`,
    data: dispatch.value,
    headers: {
      Authorization: `Bearer ${store.getters.token}`,
    },
  })
    .then((response) => {
      if (response.data.err != "1") {
        swal.close();
        toast.success("Cập nhật công văn thành công!");
        loadData(true);
        closeDialog();
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

const sttDispatch = ref();
//Thêm bản ghi con
const isChirlden = ref(false);

//Sửa bản ghi
const editDispatch = (dataPlace) => {
  submitted.value = false;
  dispatch.value = dataPlace;
  isChirlden.value = false;
  if (dataPlace.parent_id != null) {
    isChirlden.value = true;
  }
  if (typeof dataPlace.open_date == "string") {
    let arr1 = dataPlace.open_date.split("/");
    minDate.value = new Date(arr1[1] + "/" + arr1[0] + "/" + arr1[2]);
  }

  headerDialog.value = "Sửa công văn";
  issaveDispatch.value = true;
  displayBasic.value = true;
};
//Xóa bản ghi
const delDispatch = (Dispatch) => {
  swal
    .fire({
      title: "Thông báo",
      text: "Bạn có muốn xoá công văn này không!",
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
          .delete(baseURL + "/api/ca_dispatch_books/Delete_dispatch_books", {
            headers: { Authorization: `Bearer ${store.getters.token}` },
            data: Dispatch != null ? [Dispatch.dispatch_book_id] : 1,
          })
          .then((response) => {
            swal.close();
            if (response.data.err != "1") {
              swal.close();
              toast.success("Xoá công văn thành công!");
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

//Xuất excel
const menuButs = ref();
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
      exportData("ImportExcel");
    },
  },
]);
const toggleExport = (event) => {
  menuButs.value.toggle(event);
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
        excelname: "DANH SÁCH CÔNG VĂN",
        proc: "ca_dispatch_book_listexport",
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

const filterSQL = ref([]);
const isFirst = ref(true);
const loadDataSQL = () => {
  let data = {
    id: options.value.id,
    next: options.value.IsNext,
    sqlO: options.value.sort,
    Search: options.value.SearchText,
    PageNo: options.value.PageNo,
    PageSize: options.value.PageSize,
    fieldSQLS: filterSQL.value,
  };

  options.value.loading = true;
  axios
    .post(baseURL + "/api/SQL/Filter_Dispatch", data, config)
    .then((response) => {
      let dt = JSON.parse(response.data.data);
      let data = dt[0];
      if (data.length > 0) {
        data.forEach((element, i) => {
          element.is_order =
            (options.value.PageNo - 1) * options.value.PageSize + i + 1;
        });

        datalists.value = data;
      } else {
        datalists.value = [];
      }
      if (isFirst.value) isFirst.value = false;
      options.value.loading = false;
      //Show Count nếu có
      if (dt.length == 2) {
        options.value.totalRecords = dt[1][0].totalRecords;
      }
    })
    .catch((error) => {
      options.value.loading = false;
      toast.error("Tải dữ liệu không thành công!");
      addLog({
        title: "Lỗi Console loadData",
        controller: "SQLView.vue",
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
//Tìm kiếm
const searchDispatchs = (event) => {
  if (event.code == "Enter") {
    options.value.loading = true;
    loadData(true);
  }
};
const onFilter = (event) => {
  filterSQL.value = [];

  for (const [key, value] of Object.entries(event.filters)) {
    if (key != "global") {
      let obj = {
        key: key != "dispatch_book_name" ? "dispatch_book_name" : key,
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
  options.value.PageNo = 1;
  options.value.id = null;
  isDynamicSQL.value = true;
  loadDataSQL();
};
//Checkbox
const onCheckBox = (value) => {
  let data = {
    IntID: value.dispatch_book_id,
    TextID: value.dispatch_book_id + "",
    IntTrangthai: 1,
    BitTrangthai: value.status,
  };
  axios
    .put(
      baseURL + "/api/ca_dispatch_books/Update_StatusDispatch_Books",
      data,
      config
    )
    .then((response) => {
      if (response.data.err != "1") {
        swal.close();
        toast.success("Cập nhật trạng thái thành công!");
        loadData(true);
        closeDialog();
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
//Xóa nhiều
const deleteList = () => {
  let listId = new Array(selectedDispatchs.length);
  swal
    .fire({
      title: "Thông báo",
      text: "Bạn có muốn xoá danh sách này không!",
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
        selectedDispatchs.value.forEach((item) => {
          listId.push(item.dispatch_book_id);
        });
        axios
          .delete(baseURL + "/api/ca_dispatch_books/Delete_dispatch_books", {
            headers: { Authorization: `Bearer ${store.getters.token}` },
            data: listId != null ? listId : 1,
          })
          .then((response) => {
            swal.close();
            if (response.data.err != "1") {
              swal.close();
              toast.success("Xoá danh sách thành công!");
              checkDelList.value = false;

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
const searchDispatch = () => {
  options.value.loading = true;
  loadData(true);
};

//Filter
const showFilter = ref(false);
const trangThai = ref([
  { name: "Có", code: 1 },
  { name: "Không", code: 0 },
]);
const phanLoai = ref([
  { name: "Hệ thống", code: 0 },
  { name: "Đơn vị", code: 1 },
]);
const filterPhanloai = ref();
const filterTrangthai = ref();

const reFilterEmail = (event) => {
  filterPhanloai.value = null;
  filterTrangthai.value = null;
  filterEmails();
   op.value.hide(event);
  showFilter.value = false;
};
const filterEmails = (event) => {
  filterSQL.value = [];
  let arr = [];
  let obj = {};
  let obj1 = {};
  if (filterPhanloai.value != null) {
    obj.key = "organization_id";
    obj.filteroperator = "and";
    arr.push({
      matchMode: "equals",
      value: filterPhanloai.value,
    });
    obj.filterconstraints = arr;
    filterSQL.value.push(obj);
  }
  if (filterTrangthai.value != null) {
    obj1.key = "status";
    obj1.filteroperator = "and";
    arr = [];
    arr.push({
      matchMode: "equals",
      value: filterTrangthai.value,
    });
    obj1.filterconstraints = arr;
    filterSQL.value.push(obj1);
  }
  options.value.PageNo = 1;
  options.value.id = null;
  loadDataSQL();
    op.value.hide(event);

};
watch(selectedDispatchs, () => {
  if (selectedDispatchs.value.length > 0) {
    checkDelList.value = true;
  } else {
    checkDelList.value = false;
  }
});
const op = ref();
const toggle = (event) => {
  op.value.toggle(event);
};

onMounted(() => {
  store.commit("setisadmin", true);
  loadData(true);
  return {
    datalists,
    options,
    onPage,
    loadData,
    loadCount,
    openBasic,
    closeDialog,
    basedomainURL,
    saveDispatch,
    isFirst,
    searchDispatchs,
    onCheckBox,
    selectedDispatchs,
    deleteList,
  };
});
</script>
<template>
  <div class="surface-100">
    <div class="h-2rem p-3 pb-0 m-3 mb-0 surface-0">
      <h3 class="m-0">
        <i class="pi pi-file-pdf"></i> Danh sách công văn ({{
          options.totalRecords
        }})
      </h3>
    </div>
    <Toolbar class="outline-none mx-3 surface-0 border-none">
      <template #start>
        <span class="p-input-icon-left">
          <i class="pi pi-search" />
          <InputText
            v-model="options.SearchText"
            @keyup="searchDispatchs"
            type="text"
            spellcheck="false"
            placeholder="Tìm kiếm"
          />
    
          <Button
            type="button"
            class="ml-2 p-button-outlined p-button-secondary"
            icon="pi pi-filter"
            @click="toggle"
            aria:haspopup="true"
            aria-controls="overlay_panel"
          />
          <OverlayPanel
            ref="op"
            appendTo="body"
            class="p-0 m-0"
            :showCloseIcon="false"
            id="overlay_panel"
            style="width: 300px"
           
          >
            <div class="grid formgrid m-0">
              <div class="flex field col-12 p-0">
                <div class="col-4 text-left pt-2 p-0" style="text-align: left">
                  Phân loại
                </div>
                <div class="col-8">
                  <Dropdown
                    class="col-12 p-0 m-0"
                    v-model="filterPhanloai"
                    :options="phanLoai"
                    optionLabel="name"
                    optionValue="code"
                    placeholder="Phân loại"
                  />
                </div>
              </div>

              <div class="flex field col-12 p-0">
                <div class="col-4 text-left pt-2 p-0" style="text-align: left">
                  Trạng thái
                </div>
                <div class="col-8">
                  <Dropdown
                    class="col-12 p-0 m-0"
                    v-model="filterTrangthai"
                    :options="trangThai"
                    optionLabel="name"
                    optionValue="code"
                    placeholder="Trạng thái"
                  />
                </div>
              </div>
              <div class="flex col-12 p-0">
                <Toolbar class="border-none surface-0 outline-none pb-0 w-full">
                  <template #start>
                    <Button
                      @click="reFilterEmail"
                      class="p-button-outlined"
                      label="Xóa"
                    ></Button>
                  </template>
                  <template #end>
                    <Button @click="filterEmails" label="Lọc"></Button>
                  </template>
                </Toolbar>
              </div>
            </div>
          </OverlayPanel>
        </span>
      </template>

      <template #end>
        <Button
          v-if="checkDelList"
          @click="deleteList()"
          label="Xóa"
          icon="pi pi-trash"
          class="mr-2 p-button-danger"
        />
        <Button
          @click="openBasic('Thêm sổ công văn')"
          label="Thêm mới"
          icon="pi pi-plus"
          class="mr-2"
        />
        <Button
          @click="searchDispatch"
          class="mr-2 p-button-outlined p-button-secondary"
          icon="pi pi-refresh"
        />

        <Button
          label="Tiện ích"
          icon="pi pi-file-excel"
          class="mr-2 p-button-outlined p-button-secondary"
          @click="toggleExport"
          aria-haspopup="true"
          aria-controls="overlay_Export"
        />
        <Menu
          id="overlay_Export"
          ref="menuButs"
          :model="itemButs"
          :popup="true"
        />
      </template>
    </Toolbar>
    <div class="d-lang-table ml-3">
      <DataTable
        :value="datalists"
        :paginator="options.totalRecords > options.PageSize"
        :rows="options.PageSize"
        :rowsPerPageOptions="[5, 10, 15, 20]"
        :scrollable="true"
        scrollHeight="flex"
        :loading="options.loading"
        v-model:selection="selectedDispatchs"
        :lazy="true"
        @page="onPage($event)"
        @filter="onFilter($event)"
        @sort="onSort($event)"
        :totalRecords="options.totalRecords"
        dataKey="dispatch_book_id"
        :rowHover="true"
        v-model:filters="filters"
        filterDisplay="menu"
        :showGridlines="true"
        filterMode="lenient"
        paginatorTemplate="FirstPageLink PrevPageLink PageLinks  NextPageLink LastPageLink"
        responsiveLayout="scroll"
        :globalFilterFields="['dispatch_book_name']"
      >
        <Column
          selectionMode="multiple"
          headerStyle="text-align:center;max-width:75px;height:50px"
          bodyStyle="text-align:center;max-width:75px;max-height:60px"
          class="align-items-center justify-content-center text-center"
        ></Column>
        <Column
          field="is_order"
          header="STT"
          :sortable="true"
          headerStyle="text-align:center;max-width:75px;height:50px"
          bodyStyle="text-align:center;max-width:75px;max-height:60px"
          class="align-items-center justify-content-center text-center"
        >
        </Column>

        <Column
          field="dispatch_book_name"
          header="Tên sổ"
          :sortable="true"
          headerStyle="height:50px"
          bodyStyle="max-height:60px"
        >
          <template #body="{ data }">
            {{ data.dispatch_book_name }}
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
          field="year"
          header="Năm"
          class="align-items-center justify-content-center text-center"
          headerStyle="text-align:center;max-width:100px;height:50px"
          bodyStyle="text-align:center;max-width:100px;;max-height:60px"
        >
        </Column>
        <Column
          field="current_num"
          header="Số hiện tại"
          class="align-items-center justify-content-center text-center"
          headerStyle="text-align:center;max-width:150px;height:50px"
          bodyStyle="text-align:center;max-width:150px;;max-height:60px"
        >
        </Column>
        <Column
          field="tracking_place"
          header="Nơi theo dõi"
          class="align-items-center justify-content-center text-center"
          headerStyle="text-align:center;max-width:150px;height:50px"
          bodyStyle="text-align:center;max-width:150px;;max-height:60px"
        >
        </Column>

        <Column
          field="status"
          header="Hiển thị"
          headerStyle="text-align:center;max-width:120px;height:50px"
          bodyStyle="text-align:center;max-width:120px;;max-height:60px"
          class="align-items-center justify-content-center text-center"
        >
          <template #body="data">
            <Checkbox
              :binary="data.data.status"
              v-model="data.data.status"
              @click="onCheckBox(data.data)"
            />
          </template>
        </Column>
        <Column
          field="organization_id"
          header="Hệ thống"
          headerStyle="text-align:center;max-width:125px;height:50px"
          bodyStyle="text-align:center;max-width:125px;;max-height:60px"
          class="align-items-center justify-content-center text-center"
        >
          <template #body="data">
            <div v-if="data.data.organization_id == 0">
              <i
                class="pi pi-check text-blue-400"
                style="font-size: 1.5rem"
              ></i>
            </div>
            <div v-else></div>
          </template>
        </Column>
        <Column
          header="Chức năng"
          class="align-items-center justify-content-center text-center"
          headerStyle="text-align:center;max-width:120px;height:50px"
          bodyStyle="text-align:center;max-width:120px;;max-height:60px"
        >
          <template #body="Dispatch">
            <Button
              @click="editDispatch(Dispatch.data)"
              class="p-button-rounded p-button-secondary p-button-outlined mx-1"
              type="button"
              icon="pi pi-pencil"
            ></Button>
            <Button
              @click="delDispatch(Dispatch.data, true)"
              class="p-button-rounded p-button-secondary p-button-outlined mx-1"
              type="button"
              icon="pi pi-trash"
            ></Button>
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
            <img src="../../assets/background/nodata.png" height="144" />
            <h3 class="m-1">Không có dữ liệu</h3>
          </div>
        </template>
      </DataTable>
    </div>
  </div>
  <Dialog
    :header="headerDialog"
    v-model:visible="displayBasic"
    :style="{ width: '50vw' }"
  >
    <form>
      <div class="grid formgrid m-2">
        <div style="display: flex" class="field col-12 md:col-12">
          <div class="col-12 p-0">
            <div class="col-12 p-0 flex my-2">
              <div class="col-2 text-left p-0 pb-2 line-height-4">
                Tên sổ <span class="redsao">(*)</span>
              </div>
              <InputText
                v-model="dispatch.dispatch_book_name"
                spellcheck="false"
                class="col-10 p-0 m-0 ip36 px-2"
              />
            </div>
            <div class="col-12 flex p-0">
              <div class="col-2"></div>
              <small
                v-if="
                  (v$.dispatch_book_name.$invalid && submitted) ||
                  v$.dispatch_book_name.$pending.$response
                "
                class="col-10 p-error p-0"
              >
                <span class="col-12 p-0">{{
                  v$.dispatch_book_name.required.$message
                    .replace("Value", "Tên sổ công văn")
                    .replace("is required", "không được để trống")
                }}</span>
              </small>
            </div>
            <div class="col-12 p-0 flex">
              <div class="col-6 p-0 my-3 flex">
                <label class="line-height-4 col-4 p-0">Năm</label>
                <InputNumber
                  :useGrouping="false"
                  v-model="dispatch.year"
                  spellcheck="false"
                  class="col-8 p-0 ip36"
                />
              </div>
              <div class="col-6 p-0 my-3 flex">
                <label class="line-height-4 col-4">Số hiện tại</label>
                <InputNumber
                  :useGrouping="false"
                  v-model="dispatch.current_num"
                  spellcheck="false"
                  class="col-8 p-0 ip36"
                />
              </div>
            </div>
            <div class="col-12 p-0 flex">
              <div class="col-6 p-0 my-3 flex">
                <label class="line-height-4 col-4 p-0">Ngày mở</label>
                <Calendar
                  class="col-8 p-0"
                  :showIcon="true"
                  autocomplete="on"
                  v-model="dispatch.open_date"
                />
              </div>
              <div class="col-6 p-0 my-3 flex">
                <label class="line-height-4 col-4">Ngày đóng</label>
                <Calendar
                  class="col-8 p-0"
                  :showIcon="true"
                  autocomplete="on"
                  v-model="dispatch.end_date"
                  :min-date="minDate ? minDate : false"
                />
              </div>
            </div>

            <div class="col-12 p-0 my-3 flex">
              <div class="pb-2 col-2 p-0 line-height-4">Nơi theo dõi</div>
              <InputText
                class="col-10 ip36 px-2"
                v-model="dispatch.tracking_place"
              />
            </div>
            <div class="col-12 p-0 my-3 flex">
              <div class="pb-2 col-2 p-0 line-height-4">Phòng ban</div>
              <Dropdown
                class="col-10"
                v-model="dispatch.department_id"
                :options="departmentOptions"
                optionLabel="name"
                optionValue="code"
                placeholder="Chọn phòng ban"
                :filter="true"
              />
            </div>
            <div class="col-12 p-0 my-3 flex">
              <div class="pb-2 col-2 p-0 line-height-4">Loại văn bản</div>
              <Dropdown
                class="col-10"
                v-model="dispatch.nav_type"
                :options="typeOptions"
                optionLabel="name"
                placeholder="Chọn loại văn bản"
                optionValue="code"
                :filter="true"
              />
            </div>
            <div class="col-12 p-0 my-3 flex">
              <div class="col-6 flex p-0">
                <div class="pb-2 col-4 p-0 line-height-4">STT</div>
                <InputNumber
                  v-model="dispatch.is_order"
                  class="col-8 p-0 ip36"
                />
              </div>
              <div class="col-6 flex p-0">
                <div class="pb-2 col-4 p-0 line-height-4 px-2">Trạng thái</div>
                <InputSwitch v-model="dispatch.status" class="col-6 p-0 ip36" />
              </div>
            </div>
          </div>
        </div>
      </div>
    </form>
    <template #footer>
      <Button
        label="Hủy"
        icon="pi pi-times"
        @click="closeDialog"
        class="p-button-text"
      />

      <Button
        label="Lưu"
        icon="pi pi-check"
        @click="saveDispatch(!v$.$invalid)"
      />
    </template>
  </Dialog>
</template>

<style scoped>
.d-lang-table {
  height: calc(100vh - 170px);
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