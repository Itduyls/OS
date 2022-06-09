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
  name: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.STARTS_WITH }],
  },
});
const rules = {
  name: {
    required,
    $errors: [
      {
        $property: "name",
        $validator: "required",
        $message: "Tên địa danh không được để trống!",
      },
    ],
  },
};
const place = ref({
  name: "",
  status: true,
  grade: 1,
  is_order: 1,
});
const selectedLangs = ref();
const submitted = ref(false);
const v$ = useVuelidate(rules, place);
const isSavePlace = ref(false);
const datalists = ref();
const toast = useToast();
const basedomainURL = baseURL;
const checkDelList = ref(false);
const selectedKeys = ref([]);
const options = ref({
  IsNext: true,
  sort: "place_id",
  SearchText: "",
  PageNo: 0,
  PageSize: 20,
  FilterUsers_ID: null,
  loading: true,
  totalRecords: null,
});
const onNodeSelect = (node) => {
  if (expandedKeys.value[node.data.place_id] == true) {
    expandedKeys.value[node.data.place_id] = false;
  } else {
    expandedKeys.value[node.data.place_id] = true;
  }
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "ca_child_list",
        par: [
          { par: "search", va: null },
          { par: "parent_id", va: node.data.place_id },
        ],
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data)[0];
      data.forEach((element) => {
        if (element.grade == 1) {
          element.grade = "Tỉnh/Thành phố";
        } else {
          if (element.grade == 2) {
            element.grade = "Quận/Huyện";
          } else {
            element.grade = "Xã/Phường";
          }
        }
      });
      if (
        data.length > 0 &&
        listdata.value.filter((x) => x.place_id == data[0].place_id).length == 0
      ) {
        listdata.value = listdata.value.concat(data);

        renderTree(listdata.value);
        expandedKeys.value[node.data.place_id] = true;
      }
    })
    .catch((error) => {
      addLog({
        title: "Lỗi Console loadCount",
        controller: "Places.vue",
        logcontent: error.message,
        loai: 2,
      });
    });
};
const onNodeUnselect = (node) => {
  selectedNodes.value.splice(selectedNodes.value.indexOf(node.data.Menu_ID), 1);
};
const expandedKeys = ref({});
//Thêm log
const addLog = (log) => {
  axios.post(baseURL + "/api/Proc/AddLog", log, config);
};
const addUserLog = (log) => {
  axios.post(baseURL + "/api/Proc/AddUserLog", log, config);
};
//Lấy số bản ghi
const loadCount = () => {
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "ca_places_count",
        par: [{ par: "parent_id", va: null }, { par: "search", va: options.value.SearchText }],   
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data)[0];
      if (data.length > 0) {
        options.value.totalRecords = data[0].totalRecords;
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
const listdata = ref();
//Lấy dữ liệu ngôn ngữ
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
          proc: "ca_places_list",
          par: [
             { par: "pageno", va: options.value.PageNo },
            { par: "pagesize", va: options.value.PageSize },
            { par: "search", va: options.value.SearchText },
            { par: "status", va: options.value.Status }
          ],
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];
        if (isFirst.value) isFirst.value = false;
        data.forEach((element) => {
          if (element.grade == 1) {
            element.grade = "Tỉnh/Thành phố";
          } else {
            if (element.grade == 2) {
              element.grade = "Quận/huyện";
            } else {
              element.grade = "Xã/Phường";
            }
          }
        });
        renderTree(data);
        listdata.value = data;
        options.value.loading = false;
      })
      .catch((error) => {
        toast.error("Tải dữ liệu không thành công!");
        options.value.loading = false;
        addLog({
          title: "Lỗi Console loadData",
          controller: "PlacesView.vue",
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

    options.value.id = datalists.value[datalists.value.length - 1].place_id;
    options.value.IsNext = true;
  } else if (event.page < options.value.PageNo) {
    //Trang trước
    options.value.id = datalists.value[0].place_id;
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
  place.value = {
    name: "",
    is_order: true,
    status: true,
    grade: 1,
  };
  isSavePlace.value = false;
  isChirlden.value = false;
  headerDialog.value = str;
  displayBasic.value = true;
};
const closeDialog = () => {
  place.value = {
    name: "",
    is_order: 1,
    status: true,
  };
  displayBasic.value = false;
};

const renderTree = (data) => {
  let arrChils = [];
  let arrtreeChils = [];
  data
    .filter((x) => x.parent_id == null)
    .forEach((m, i) => {
      m.is_order = i + 1;
      let om = { key: m.place_id, data: m };
      const rechildren = (mm, place_id) => {
        let dts = data.filter((x) => x.parent_id == place_id);
        if (dts.length > 0) {
          if (!mm.children) mm.children = [];
          dts.forEach((em, j) => {
            let om1 = { key: em.place_id, data: em };
            om1.data.is_order = j + 1;
            rechildren(om1, em.place_id);
            mm.children.push(om1);
          });
        }
      };
      rechildren(om, m.place_id);

      arrChils.push(om);
      om = { key: m.place_id, data: m.place_id, label: m.name };
      const retreechildren = (mm, place_id) => {
        let dts = data.filter((x) => x.parent_id == place_id);
        if (dts.length > 0) {
          if (!mm.children) mm.children = [];
          dts.forEach((em) => {
            let om1 = {
              key: em.place_id,
              data: em.place_id,
              label: em.name,
            };
            retreechildren(om1, em.place_id);
            mm.children.push(om1);
          });
        }
      };
      retreechildren(om, m.place_id);
      arrtreeChils.push(om);
    });
  arrtreeChils.unshift({ key: -1, data: -1, label: "-----Chọn Module----" });

  datalists.value = arrChils;
};
const handleFileUpload = (event) => {
  files = event.target.files;
  var output = document.getElementById("logoLang");
  output.src = URL.createObjectURL(event.target.files[0]);
  output.onload = function () {
    URL.revokeObjectURL(output.src); // free memory
  };
};
//Thêm bản ghi
const savePlace = (isFormValid) => {
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
  if (!isSavePlace.value) {
    axios
      .post(baseURL + "/api/ca_places/Add_ca_places", place.value, config)
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Thêm địa danh thành công!");
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
      .put(baseURL + "/api/ca_places/Update_ca_places", place.value, config)
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
            toast.success("Sửa địa danh thành công!");
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

const sttChirl = ref();
const nameParent = ref();
const idParent = ref();
//Thêm bản ghi con
const AddChirl = (value) => {
  (async () => {
    await axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "ca_places_count",
           par: [{ par: "parent_id", va: null }, { par: "search", va: options.value.SearchText }],   
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];
        sttChirl.value = data[0].totalRecords + 1;
      })
      .catch((error) => {
        toast.error("Tải dữ liệu không thành công!");
        options.value.loading = false;
        console.log(error.message);
        addLog({
          title: "Lỗi Menu Count",
          controller: "PlaceView.vue",
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
    place.value = {
      name: "",
      status: true,
      grade: value.grade + 1,
      parent_id: value.place_id,
      is_order: sttChirl.value,
    };
    nameParent.value = value.name;
    isChirlden.value = true;
    submitted.value = false;
    headerDialog.value = "Thêm địa danh con";
    isSavePlace.value = false;
    idParent.value = value.place_id;
    displayBasic.value = true;
  })();
};
const isChirlden = ref(false);

//Sửa bản ghi
const editPlace = (dataPlace) => {
  submitted.value = false;
  place.value = dataPlace;
  isChirlden.value = false;
  if (dataPlace.parent_id != null) {
    isChirlden.value = true;
  }
  headerDialog.value = "Sửa địa danh";
  isSavePlace.value = true;
  displayBasic.value = true;
};
//Xóa bản ghi
const delPlace = (Place) => {
  swal
    .fire({
      title: "Thông báo",
      text: "Bạn có muốn xoá địa danh này không!",
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
          .delete(baseURL + "/api/ca_places/Delete_ca_places", {
            headers: { Authorization: `Bearer ${store.getters.token}` },
            data: Place != null ? [Place.place_id] : 1,
          })
          .then((response) => {
            swal.close();
            if (response.data.err != "1") {
              swal.close();
              toast.success("Xoá địa danh thành công!");
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
  },   {
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
        excelname: "DANH SÁCH ĐỊA DANH",
        proc: "ca_places_listexport",
        par: [{ par: "Search", va: options.value.SearchText }],
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
  if (event.sortField != "place_id") {
    options.value.sort +=
      ",place_id " + (event.sortOrder == 1 ? " ASC" : " DESC");
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
    .post(baseURL + "/api/SQL/Filter_Places", data, config)
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
const searchPlaces = () => {
  isDynamicSQL.value = false;
  loadData(true);
};
const onFilter = (event) => {
  filterSQL.value = [];

  for (const [key, value] of Object.entries(event.filters)) {
    if (key != "global") {
      let obj = {
        key: key != "Name" ? "Macode" : key,
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
  expandedKeys.value[value.place_id] = false;
  let data = {
    IntID: value.place_id,
    TextID: value.place_id + "",
    IntTrangthai: 1,
    BitTrangthai: value.status,
  };
  axios
    .put(baseURL + "/api/ca_places/Update_StatusPlace", data, config)
    .then((response) => {
      if (response.data.err != "1") {
        swal.close();
        toast.success("Sửa địa danh thành công!");
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
  let listId = new Array(selectedLangs.length);

  swal
    .fire({
      title: "Thông báo",
      text: "Bạn có muốn xoá ngôn ngữ này không!",
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
        selectedLangs.value.forEach((item) => {
          if (item.IsMain) {
            toast.error("Không được xóa ngôn ngữ chính!");
            return;
          }
        });
        selectedLangs.value.forEach((item) => {
          listId.push(item.ID);
        });
        axios
          .delete(baseURL + "/api/CMS_Lang/DeleteCMS_Lang", {
            headers: { Authorization: `Bearer ${store.getters.token}` },
            data: listId != null ? listId : 1,
          })
          .then((response) => {
            swal.close();
            if (response.data.err != "1") {
              swal.close();
              toast.success("Xoá ngôn ngữ thành công!");
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
watch(selectedLangs, () => {
  if (selectedLangs.value.length > 0) {
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
    savePlace,
    isFirst,
    searchPlaces,
    onCheckBox,
    selectedLangs,
    deleteList,
  };
});
</script>
<template>
  <div class="surface-100">
    <div class="h-2rem p-3 pb-0 m-3 mb-0 surface-0">
      <h3 class="m-0">
        <i class="pi pi-globe"></i> Danh sách địa danh ({{
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
            @keyup="searchPlaces"
            type="text"
            spellcheck="false"
            placeholder="Tìm kiếm"
          />
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
          @click="openBasic('Thêm mới')"
          label="Thêm mới"
          icon="pi pi-plus"
          class="mr-2"
        />
        <Button
          @click="loadData(true)"
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
    <div class="d-lang-table mx-3">
      <TreeTable
        @nodeSelect="onNodeSelect"
        @nodeUnselect="onNodeUnselect"
        :value="datalists"
        :paginator="options.totalRecords>options.PageSize"
        paginatorTemplate="FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink  RowsPerPageDropdown"
        :rows="options.PageSize"
        :rowsPerPageOptions="[5, 10, 20, 50, 100]"
        style="margin-bottom: 2rem"
        :scrollable="true"
        scrollHeight="flex"
        selectionMode="single"
        v-model:selectionKeys="selectedKeys"
        :loading="options.loading"
        :expandedKeys="expandedKeys"
         @page="onPage($event)"
      
        @sort="onSort($event)"
         :rowHover="true"
      
  
        :showGridlines="true"
   
        responsiveLayout="scroll"
       
      >
        <Column
          field="is_order"
          header="STT"
          :sortable="true"
          headerStyle="text-align:center;max-width:75px;height:50px"
          bodyStyle="text-align:center;max-width:75px;;max-height:60px"
          class="align-items-center justify-content-center text-center"
        >
          <template #body="menu">
            <div
              v-if="menu.node.data.parent_id == null"
              style="font-weight: 700"
            >
              {{ menu.node.data.is_order }}
            </div>
            <div v-else style="font-size: 16px">
              {{ menu.node.data.is_order }}
            </div>
          </template>
        </Column>

        <Column
          field="name"
          header="Tên địa danh"
          :expander="true"
          :sortable="true"
          headerStyle="height:50px"
          bodyStyle="max-height:60px"
        >
      </Column>

        <Column
          field="grade"
          header="Cấp hành chính"
          headerStyle="text-align:center;max-width:200px;height:50px"
          bodyStyle="text-align:center;max-width:200px;max-height:60px"
          class="align-items-center justify-content-center text-center"
        ></Column>

        <Column
          field="status"
           header="Hiển thị"
          headerStyle="text-align:center;max-width:120px;height:50px"
          bodyStyle="text-align:center;max-width:120px;;max-height:60px"
          class="align-items-center justify-content-center text-center"
        >
          <template #body="data">
            <Checkbox
              :binary="data.node.data.status"
              v-model="data.node.data.status"
              @click="onCheckBox(data.node.data)"
            /> </template
        ></Column>
        <Column
          header="Chức năng"
          class="align-items-center justify-content-center text-center"
          headerStyle="text-align:center;max-width:200px;height:50px"
          bodyStyle="text-align:center;max-width:200px;;max-height:60px"
        >
          <template #body="Place">
            <Button
              @click="AddChirl(Place.node.data)"
              class="p-button-rounded p-button-secondary p-button-outlined mx-1"
              type="button"
              icon="pi pi-plus-circle"
            ></Button>
            <Button
              @click="editPlace(Place.node.data)"
              class="p-button-rounded p-button-secondary p-button-outlined mx-1"
              type="button"
              icon="pi pi-pencil"
            ></Button>
            <Button
              @click="delPlace(Place.node.data, true)"
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
      </TreeTable>
    </div>
  </div>
  <Dialog
    :header="headerDialog"
    v-model:visible="displayBasic"
    :style="{ width: '40vw' }"
  >
    <form>
      <div class="grid formgrid m-2">
        <div v-if="isChirlden" class="field col-12 md:col-12">
          <label class="col-3 text-left p-0"
            >Cấp cha<span class="redsao"></span
          ></label>
          <InputText v-model="nameParent" :disabled="true" class="col-8 ip36 px-2" />
        </div>
        <div class="field col-12 md:col-12">
          <label class="col-3 text-left p-0"
            >Tên địa danh <span class="redsao">(*)</span></label
          >
          <InputText
            v-model="place.name"
            spellcheck="false"
            class="col-8 ip36 px-2"
            :class="{ 'p-invalid': v$.name.$invalid && submitted }"
          />
        </div>
        <div style="display: flex" class="field col-12 md:col-12">
          <div class="col-4 text-left"></div>
          <small
            v-if="(v$.name.$invalid && submitted) || v$.name.$pending.$response"
            class="col-8 p-error"
          >
            <span class="col-12 p-0">{{
              v$.name.required.$message
                .replace("Value", "Tên địa danh")
                .replace("is required", "không được để trống")
            }}</span>
          </small>
        </div>
        <div style="display: flex" class="col-12 field md:col-12">
          <div class="field col-6 md:col-6 p-0">
            <label class="col-6 text-left p-0">Số thứ tự </label>
            <InputNumber v-model="place.is_order" class="col-6 ip36 p-0" />
          </div>
          <div class="field col-6 md:col-6 p-0">
            <label
              style="vertical-align: text-bottom"
              class="col-6 text-center p-0"
              >Trạng thái
            </label>
            <InputSwitch v-model="place.status" class="col-6" />
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

      <Button label="Lưu" icon="pi pi-check" @click="savePlace(!v$.$invalid)" />
    </template>
  </Dialog>
</template>

<style scoped>
.d-lang-table {
  height: calc(100vh - 170px);
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