<script setup>
import { ref, inject, onMounted, watch } from "vue";
import { useToast } from "vue-toastification";
import { required } from "@vuelidate/validators";
import { useVuelidate } from "@vuelidate/core";
import { FilterMatchMode, FilterOperator } from "primevue/api";
const axios = inject("axios");
const store = inject("store");
const swal = inject("$swal");
const isDynamicSQL = ref(false);
const config = {
  headers: { Authorization: `Bearer ${store.getters.token}` },
};
const filters = ref({
  global: { value: null, matchMode: FilterMatchMode.CONTAINS },
  stamp_name: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.STARTS_WITH }],
  },
});
const rules = {
  stamp_name: {
    required,
    $errors: [
      {
        $property: "stamp_name",
        $validator: "required",
        $message: "Tên tem không được để trống!",
      },
    ],
  },
};
const stamp = ref({
  stamp_name: "",
  image: "",
  status: true,
  is_default: false,
  is_order: 1,
});
const selectedStamps = ref();
const submitted = ref(false);
const v$ = useVuelidate(rules, stamp);
const isSaveTem = ref(false);
const datalists = ref();
const toast = useToast();
const basedomainURL = baseURL;
const checkDelList = ref(false);

const options = ref({
  IsNext: true,
  sort: "stamp_id",
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
        proc: "ca_stamp_count",par: [
            { par: "search", va: options.value.SearchText },
            
          ],
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data)[0];
      if (data.length > 0) {
        options.value.totalRecords = data[0].totalRecords;
        sttStamp.value = data[0].totalRecords + 1;
      }
    })
    .catch((error) => {
      addLog({
        title: "Lỗi Console loadCount",
        controller: "TemView.vue",
        log_content: error.message,
      });
    });
};
//Lấy dữ liệu tem
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
          proc: "ca_stamp_list",
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
        
        datalists.value = data;
        options.value.loading = false;
      })
      .catch((error) => {
        toast.error("Tải dữ liệu không thành công!");
        options.value.loading = false;
        addLog({
          title: "Lỗi Console loadData",
          controller: "TemView.vue",
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
//Phân trang dữ liệu
const onPage = (event) => {
  if (event.page == 0) {
    //Trang đầu
    options.value.id = null;
    options.value.IsNext = true;
  } else if (event.page > options.value.PageNo + 1) {
    //Trang cuối
    options.value.id = -1;
    options.value.IsNext = false;
  } else if (event.page > options.value.PageNo) {
    //Trang sau

    options.value.id = datalists.value[datalists.value.length - 1].stamp_id;
    options.value.IsNext = true;
  } else if (event.page < options.value.PageNo) {
    //Trang trước
    options.value.id = datalists.value[0].stamp_id;
    options.value.IsNext = false;
  }
  options.value.PageNo = event.page;
  loadData(true);
};
//Hiển thị dialog
const headerDialog = ref();
const displayBasic = ref(false);
const openBasic = (str) => {
  submitted.value = false;
  stamp.value = {
    stamp_name: "",
    image: "",
    status: true,
    is_default: false,
    is_order: sttStamp.value,
  };
  if (store.state.user.IsAdmin) {
    stamp.value.organization_id = 0;
  }
  checkIsmain.value = false;
  isSaveTem.value = false;
  headerDialog.value = str;
  displayBasic.value = true;
};
const closeDialog = () => {
  stamp.value = {
    stamp_name: "",
    image: "",
    status: true,
    is_default: false,
    is_order: 1,
  };
  displayBasic.value = false;
};
//Lấy file logo
const chonanh = (id) => {
  document.getElementById(id).click();
};
const handleFileUpload = (event) => {
  files = event.target.files;
  var output = document.getElementById("logoTem");
  output.src = URL.createObjectURL(event.target.files[0]);
  output.onload = function () {
    URL.revokeObjectURL(output.src); // free memory
  };
};
//Thêm bản ghi
let files = [];
const sttStamp = ref(1);
const saveData = (isFormValid) => {
  submitted.value = true;
  if (!isFormValid) {
    return;
  }
  let formData = new FormData();
  for (var i = 0; i < files.length; i++) {
    let file = files[i];
    formData.append("islogo", file);
  }

  formData.append("stamp", JSON.stringify(stamp.value));
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  if (!isSaveTem.value) {
    axios
      .post(baseURL + "/api/ca_stamps/Add_stamp", formData, config)
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Thêm tem thành công!");
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
  } else {
    axios
      .put(baseURL + "/api/ca_stamps/Update_Stamp", formData, config)
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Sửa tem thành công!");
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
  }
};
const checkIsmain = ref(true);
//Sửa bản ghi
const editTem = (dataTem) => {
  submitted.value = false;
  stamp.value = dataTem;

  if (stamp.value.is_default) {
    checkIsmain.value = false;
  } else {
    checkIsmain.value = true;
  }
  headerDialog.value = "Sửa tem";
  isSaveTem.value = true;
  displayBasic.value = true;
};
//Xóa bản ghi
const delTem = (Tem) => {
  swal
    .fire({
      title: "Thông báo",
      text: "Bạn có muốn xoá tem này không!",
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
        if (Tem.is_default) {
          toast.error("Không được xóa tem chính!");
          swal.close();
          return;
        } else {
          axios
            .delete(baseURL + "/api/ca_stamps/Delete_stamp", {
              headers: { Authorization: `Bearer ${store.getters.token}` },
              data: Tem != null ? [Tem.stamp_id] : 1,
            })
            .then((response) => {
              swal.close();
              if (response.data.err != "1") {
                swal.close();
                toast.success("Xoá tem thành công!");
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
  },  {
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
        excelname: "DANH SÁCH TEM",
        proc: "ca_stamp_listexport",
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
//Sort
const onSort = (event) => {
  options.value.sort =
    event.sortField + (event.sortOrder == 1 ? " ASC" : " DESC");
  if (event.sortField != "id") {
    options.value.sort += ",id " + (event.sortOrder == 1 ? " ASC" : " DESC");
  }
  
  isDynamicSQL.value = true;
  loadDataSQL();
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
    .post(baseURL + "/api/SQL/Filter_Stamp", data, config)
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
const searchStamp = (event) => {
  if (event.code == "Enter") {
    options.value.loading = true;
    loadData(true);
  }
};
const refreshStamp = () => {
  options.value.loading = true;
  loadData(true);
};
const onFilter = (event) => {
  filterSQL.value = [];

  for (const [key, value] of Object.entries(event.filters)) {
    if (key != "global") {
      let obj = {
        key: key != "stamp_name" ? "stamp_name" : key,
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
const onCheckBox = (value, check, checkIsmain) => {
  if (check) {
    let data = {
      IntID: value.stamp_id,
      TextID: value.stamp_id + "",
      IntTrangthai: 1,
      BitTrangthai: value.status,
    };
    axios
      .put(baseURL + "/api/ca_stamps/Update_TrangthaiStamp", data, config)
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Sửa trạng thái tem thành công!");
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
  } else {
    let data1 = {
      IntID: value.stamp_id,
      TextID: value.stamp_id + "",
      BitMain: value.is_default,
    };
    axios
      .put(baseURL + "/api/ca_stamps/Update_DefaultStamp", data1, config)
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Sửa trạng thái tem thành công!");
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
  }
};
//Xóa nhiều
const deleteList = () => {
  let listId = new Array(selectedStamps.length);
  let checkD = false;
  selectedStamps.value.forEach((item) => {
    if (item.is_default) {
      toast.error("Không được xóa tem mặc định!");
      checkD = true;
      return;
    }
  });
  if (!checkD) {
    swal
      .fire({
        title: "Thông báo",
        text: "Bạn có muốn xoá tem này không!",
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

          selectedStamps.value.forEach((item) => {
            listId.push(item.stamp_id);
          });
          axios
            .delete(baseURL + "/api/ca_stamps/Delete_stamp", {
              headers: { Authorization: `Bearer ${store.getters.token}` },
              data: listId != null ? listId : 1,
            })
            .then((response) => {
              swal.close();
              if (response.data.err != "1") {
                swal.close();
                toast.success("Xoá tem thành công!");
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
  }
};

//Filter
const showFilter = ref(false);
const toggleFilter = (event) => {
  if (showFilter.value) {
    showFilter.value = false;
  } else {
    showFilter.value = true;
  }
};
const filterButs = ref();
const itemfilterButs = ref([
  {
    label: "Phân loại",
    check: true,
  },
  {
    label: "Trạng thái",
    check: false,
  },
]);

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

const reFilterEmail = () => {
  filterPhanloai.value = null;
  filterTrangthai.value = null;
  filterFileds();
  showFilter.value = false;
};
const filterFileds = () => {
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
  options.value.PageNo = 0;
  options.value.id = null;
  loadDataSQL();
  showFilter.value = false;
};
watch(selectedStamps, () => {
  if (selectedStamps.value.length > 0) {
    checkDelList.value = true;
  } else {
    checkDelList.value = false;
  }
});

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
    handleFileUpload,
    saveData,
    isFirst,
    searchStamp,
    onCheckBox,
    selectedStamps,
    deleteList,
  };
});
</script>
<template>
  <div class="d-container">
    <div class="d-lang-header">
      <h3 class="d-module-title">
        <i class="pi pi-globe"></i> Danh sách tem ({{ options.totalRecords }})
      </h3>
    </div>
    <Toolbar class="d-toolbar">
      <template #start>
        <span class="p-input-icon-left">
          <i class="pi pi-search" />
          <InputText
            v-model="options.SearchText"
            @keyup="searchStamp"
            type="text"
            spellcheck="false"
            placeholder="Tìm kiếm"
          />
          <Button
            class="ml-2 p-button-outlined p-button-secondary"
            icon="pi pi-filter"
            @click="toggleFilter"
            aria-haspopup="true"
            aria-controls="overlay_filter"
          />
          <Menu
            style="width: 260px; position: absolute; z-index: 1000"
            id="overlay_filter"
            ref="filterButs"
            :model="itemfilterButs"
            v-if="showFilter"
          >
            <template #item="{ item }">
              <div class="grid formgrid m-2">
                <div class="field col-12 md:col-12">
                  <div v-if="item.check" class="flex col-12 p-0">
                    <div
                      class="col-4 text-left pt-2 p-0"
                      style="text-align: left"
                    >
                      {{ item.label }}:
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
                  <span v-else>
                    <div class="flex">
                      <div
                        class="col-4 text-left pt-2 p-0"
                        style="text-align: left"
                      >
                        {{ item.label }}:
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

                    <Toolbar class="border-none surface-0 outline-none pb-0">
                      <template #start>
                        <Button
                          @click="reFilterEmail"
                          class="p-button-outlined"
                          label="Xóa"
                        ></Button>
                      </template>
                      <template #end>
                        <Button @click="filterFileds" label="Lọc"></Button>
                      </template>
                    </Toolbar>
                  </span>
                </div>
              </div>
            </template>
          </Menu>
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
          @click="openBasic('Thêm Tem')"
          label="Thêm mới"
          icon="pi pi-plus"
          class="mr-2"
        />
        <Button
          @click="refreshStamp"
          class="mr-2 p-button-outlined p-button-secondary"
          icon="pi pi-refresh"
        />

        <Button
          label="Export"
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
    <div class="d-lang-table">
      <DataTable
        @page="onPage($event)"
        @sort="onSort($event)"
        @filter="onFilter($event)"
        v-model:filters="filters"
        filterDisplay="menu"
        filterMode="lenient"
        :filters="filters"
        :globalFilterFields="['stamp_name']"
        :scrollable="true"
        scrollHeight="flex"
        :showGridlines="true"
        columnResizeMode="fit"
        :lazy="true"
        :totalRecords="options.totalRecords"
        :loading="options.loading"
        :reorderableColumns="true"
        :value="datalists"
        :paginator="options.totalRecords > options.PageSize"
        v-model:rows="options.PageSize"
        paginatorTemplate="  FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink  RowsPerPageDropdown"
        :rowsPerPageOptions="[5, 10, 20, 50, 100]"
        dataKey="stamp_id"
        responsiveLayout="scroll"
        v-model:selection="selectedStamps"
      >
        <Column
          class="align-items-center justify-content-center text-center"
          headerStyle="text-align:center;max-width:70px;height:50px"
          bodyStyle="text-align:center;max-width:70px"
          selectionMode="multiple"
        >
        </Column>

        <Column
          field="is_order"
          header="STT"
          class="align-items-center justify-content-center text-center"
          headerStyle="text-align:center;max-width:120px;height:50px"
          bodyStyle="text-align:center;max-width:120px"
          :sortable="true"
        ></Column>

        <Column
          field="stamp_name"
          header="Tên Tem"
          :sortable="true"
          headerStyle="text-align:left;height:50px"
          bodyStyle="text-align:left"
        >
          <template #filter="{ filterModel }">
            <InputText
              type="text"
              v-model="filterModel.value"
              class="p-column-filter px-2"
              placeholder="Từ khoá"
            />
          </template>
        </Column>

        <Column
          field="image"
          header="Hình ảnh"
          headerStyle="text-align:center;max-width:150px;height:50px"
          bodyStyle="text-align:center;max-width:150px"
          class="align-items-center justify-content-center text-center"
        >
          <template #body="logo">
            <img
              style="object-fit: contain; border: unset; outline: unset"
              width="100"
              height="50"
              alt=" "
              v-bind:src="
                logo.data.image
                  ? basedomainURL + logo.data.image
                  : '/src/assets/image/fails.png'
              "
            />
          </template>
        </Column>
        <Column
          field="status"
           header="Hiển thị"
          headerStyle="text-align:center;max-width:150px;height:50px"
          bodyStyle="text-align:center;max-width:150px"
          class="align-items-center justify-content-center text-center"
        >
          <template #body="data">
            <Checkbox
              :binary="true"
              v-model="data.data.status"
              @click="onCheckBox(data.data, true, true)"
            /> </template
        ></Column>
        <Column
          field="is_default"
          header="Mặc định"
          headerStyle="text-align:center;max-width:150px;height:50px"
          bodyStyle="text-align:center;max-width:150px"
          class="align-items-center justify-content-center text-center"
        >
          <template #body="data">
            <Checkbox
              :binary="true"
              v-model="data.data.is_default"
              @click="onCheckBox(data.data, false, true)"
            />
          </template>
        </Column>
        <Column
          field="organization_id"
          header="Hệ thống"
          headerStyle="text-align:center;max-width:125px;height:50px"
          bodyStyle="text-align:center;max-width:125px;"
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
          bodyStyle="text-align:center;max-width:120px"
        >
          <template #body="Tem">
            <Button
              @click="editTem(Tem.data)"
              class="p-button-rounded p-button-secondary p-button-outlined mx-1"
              type="button"
              icon="pi pi-pencil"
            ></Button>
            <Button
              class="p-button-rounded p-button-secondary p-button-outlined mx-1"
              type="button"
              icon="pi pi-trash"
              @click="delTem(Tem.data)"
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
    :style="{ width: '40vw' }"
  >
    <form>
      <div class="grid formgrid m-2">
        <div class="field col-12 md:col-12">
          <label class="col-3 text-left"
            >Tên Tem <span class="redsao">(*)</span></label
          >
          <InputText
            v-model="stamp.stamp_name"
            spellcheck="false"
            class="col-9 ip36 px-2"
            :class="{ 'p-invalid': v$.stamp_name.$invalid && submitted }"
          />
        </div>
        <div style="display: flex" class="field col-12 md:col-12">
          <div class="col-3 text-left"></div>
          <small
            v-if="
              (v$.stamp_name.$invalid && submitted) ||
              v$.stamp_name.$pending.$response
            "
            class="col-9 p-error"
          >
            <span class="col-12 p-0">{{
              v$.stamp_name.required.$message
                .replace("Value", "Tên tem")
                .replace("is required", "không được để trống")
            }}</span>
          </small>
        </div>
        <div style="display: flex" class="col-12 field md:col-12">
          <div class="col-9">
            <div class="field col-12 md:col-12 p-0">
              <label class="col-4 text-left p-0">STT</label>
              <InputNumber v-model="stamp.is_order" class="col-8 ip36 p-0" />
            </div>
            <div v-if="checkIsmain" class="field col-12 md:col-12 p-0">
              <label
                style="vertical-align: text-bottom"
                class="col-4 text-left p-0"
                >Mặc định
              </label>
              <InputSwitch v-model="stamp.is_default" class="col-8" />
            </div>

            <div class="field col-12 md:col-12 p-0">
              <label
                style="vertical-align: text-bottom"
                class="col-4 text-left p-0"
                >Trạng thái
              </label>
              <InputSwitch v-model="stamp.status" class="col-8" />
            </div>
          </div>
          <div class="col-3">
            <div class="field">
              <label class="col-12 text-left p-0 pl-3">Hình ảnh</label>
              <div class="inputanh col-12" @click="chonanh('AnhTem')">
                <img
                  id="logoTem"
                  v-bind:src="
                    stamp.image
                      ? basedomainURL + stamp.image
                      : '/src/assets/image/noimg.jpg'
                  "
                />
              </div>
              <input
                class="ipnone"
                id="AnhTem"
                type="file"
                accept="image/*"
                @change="handleFileUpload"
              />
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
        @click="saveData(!v$.$invalid)"
        autofocus
      />
    </template>
  </Dialog>
</template>

<style scoped>
.d-container {
  background-color: #f5f5f5;
}

.d-lang-header {
  background-color: #ffff;
  padding: 12px 8px 0px 8px;
  margin: 8px 8px 0px 8px;
  height: 33px;
}
.d-lang-header h3,
i {
  font-weight: 600;
}
.d-module-title {
  margin: 0;
}
.d-toolbar {
  border: unset;
  outline: unset;
  background-color: #fff;
  margin: 0px 8px 0px 8px;
}
.d-lang-table {
  margin: 0px 8px 0px 8px;
  height: calc(100vh - 167px);
}
.d-btn-function {
  border-radius: 50%;
  margin-left: 6px;
}
.d-btn-delete {
  background-color: rgb(237, 114, 84);
  border: 1px solid rgb(214, 125, 125);
}
.d-btn-delete:hover {
  background-color: rgb(255, 0, 0);
  border: 1px solid rgb(214, 125, 125);
}
.d-btn-edit:hover {
  background-color: rgb(63, 46, 252);
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