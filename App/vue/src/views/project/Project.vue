<script setup>
import { ref, inject, onMounted, watch } from "vue";
import { useToast } from "vue-toastification";
import { required } from "@vuelidate/validators";
import { useVuelidate } from "@vuelidate/core";
import { FilterMatchMode, FilterOperator } from "primevue/api";
import ClassicEditor from "@ckeditor/ckeditor5-build-classic";
//Khai báo
const axios = inject("axios");
const store = inject("store");
const swal = inject("$swal");
const toast = useToast();
const isFirst = ref(true);
const basedomainURL = baseURL;


const config = {
  headers: { Authorization: `Bearer ${store.getters.token}` },
};
const headerDialog = ref();
const displayBasic = ref(false);
const isChirlden = ref(false);
const editor = ref(ClassicEditor);
const editorConfig = ref({
  toolbar: [
    "heading",
    "bold",
    "italic",
    "underline",
    "Link",
    "|",
    "alignment",
    "bulletedList",
    "numberedList",
    "|",
    "insertImage",
    "mediaEmbed",
    "horizontalLine",
    "|",
    "fontColor",
    "fontBackgroundColor",
    "fontFamily",
    "fontSize",
    "highlight",

    "|",
    "insertTable",
    "tableColumn",
    "tableRow",
    "mergeTableCells",
    "|",

    "imageStyle:inline",
    "imageStyle:block",
    "imageStyle:side",
    "toggleImageCaption",
    "imageTextAlternative",
    "|",

    "strikethrough",
    "outdent",
    "indent",
    "|",
    "codeBlock",
    "linkImage",
    "blockQuote",
    "code",
    "subscript",
    "superscript",
    "|",
    "undo",
    "redo",
    "findAndReplace",
  ],
  indentBlock: {
    offset: 1,
    unit: "em",
  },
  removePlugins: ["MediaEmbedToolbar"],
});
const renderTree = (data) => {
  let arrChils = [];
  let arrtreeChils = [];
  data
    .filter((x) => x.parent_id == null)
    .forEach((m, i) => {
      m.is_order = i + 1;
      let om = { key: m.project_id, data: m };
      const rechildren = (mm, project_id) => {
        let dts = data.filter((x) => x.parent_id == project_id);
        if (dts.length > 0) {
          if (!mm.children) mm.children = [];
          dts.forEach((em, j) => {
            let om1 = { key: em.project_id, data: em };
            om1.data.is_order = j + 1;
            rechildren(om1, em.project_id);
            mm.children.push(om1);
          });
        }
      };
      rechildren(om, m.project_id);
      arrChils.push(om);
      //
      om = { key: m.project_id, data: m.project_id, label: m.project_name };
      const retreechildren = (mm, project_id) => {
        let dts = data.filter((x) => x.parent_id == project_id);
        if (dts.length > 0) {
          if (!mm.children) mm.children = [];
          dts.forEach((em) => {
            let om1 = {
              key: em.project_id,
              data: em.project_id,
              label: em.project_name,
            };
            retreechildren(om1, em.project_id);
            mm.children.push(om1);
          });
        }
      };
      retreechildren(om, m.project_id);
      arrtreeChils.push(om);
    });
  arrtreeChils.unshift({ key: -1, data: -1, label: "-----Chọn Module----" });
  datalists.value = arrChils;
  console.log("Dữ liệu", data);
  // datalists.value = arrtreeChils;
};
const project = ref({
  project_name: "",
  status: null,
  is_order: 1,
});
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
const rules = {
  project_name: {
    required,
    $errors: [
      {
        $property: "project_name",
        $validator: "required",
        $message: "Tên dự án không được để trống!",
      },
    ],
  },
  db_name: {
    required,
    $errors: [
      {
        $property: "db_name",
        $validator: "required",
        $message: "Tên database không được để trống!",
      },
    ],
  },
};
const submitted = ref(false);
const v$ = useVuelidate(rules, project);
const selectedKeys = ref([]);
const checkDelList = ref(false);
const listLangs = ref();
const selectedNodes = ref([]);
//Method

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
        excelname: "DANH SÁCH DỰ ÁN",
        proc: "api_project_listexport",
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
//Xóa nhiều
const onNodeSelect = (node) => {
  selectedNodes.value.push(node.data.project_id);
};
const onNodeUnselect = (node) => {
  selectedNodes.value.splice(
    selectedNodes.value.indexOf(node.data.project_id),
    1
  );
};
const closeDialog = () => {
  project.value = {
    project_name: "",

    status: null,
    is_order: 1,
  };
  displayBasic.value = false;
};

//Lấy file logo
const chonanh = (id) => {
  document.getElementById(id).click();
};
let files = [];
const handleFileUpload = (event) => {
  files = event.target.files;
  var output = document.getElementById("logoTem");
  output.src = URL.createObjectURL(event.target.files[0]);
  output.onload = function () {
    URL.revokeObjectURL(output.src); // free memory
  };
};
const openBasic = (str) => {
  submitted.value = false;
  project.value = {
    project_name: "",

    status: null,
    is_order: sttProject.value,
  };


  isSaveMenu.value = false;

  isChirlden.value = false;
  headerDialog.value = str;
  displayBasic.value = true;
};
const sttProject = ref(1);

const datalists = ref([]);
const isDynamicSQL = ref(false);
const loadCount = () => {
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "api_project_count",
        par: [
          { par: "parent_id", va: options.value.parent_id },
          { par: "search", va: options.value.SearchText },
        ],
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data)[0];
      if (data.length > 0) {
        options.value.totalRecords = data[0].totalRecords;
        sttProject.value = options.value.totalRecords + 1;
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
const trangThai = ref([
  { name: "Đang lập kế hoạch", code: 0 },
  { name: "Đang thực hiện", code: 1 },
  { name: "Đã hoàn thành", code: 2 },
  { name: "Tạm dừng", code: 3 },
  { name: "Đóng", code: 4 },
]);
const listDatabase = ref([]);
const listDB = () => {
   listDatabase.value=[];
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "api_list_database",
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data)[0];
      data.forEach((element) => {
        let db = { name: element.name, code: element.name };
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
};
const expandedKeys = ref({});
//Lấy dữ liệu project
const loadData = (rf) => {
  if (rf) {
    if (isDynamicSQL.value) {
      loadDataSQL();
      return false;
    }
    if (rf) {
      if (options.value.PageNo == 0) {
        loadCount();
        listDB();
      }
    }
    axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "api_project_list",
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
        renderTree(data);

        options.value.loading = false;
      })
      .catch((error) => {
        toast.error("Tải dữ liệu không thành công!");
        options.value.loading = false;
        addLog({
          title: "Lỗi Console loadData",
          controller: "MenuView.vue",
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
const onPage = (event) => {
  options.value.id = null;
  options.value.IsNext = true;
  options.value.PageNo = event.page + 1;

  loadDataSQL();
};
const onSort = (event) => {
  options.value.sort =
    event.sortField + (event.sortOrder == 1 ? " ASC" : " DESC");
  if (event.sortField != "project_id") {
    options.value.sort +=
      ",project_id " + (event.sortOrder == 1 ? " ASC" : " DESC");
  }
  options.value.PageNo = 1;
  checkSort.value = true;
  isDynamicSQL.value = true;
  loadDataSQL();
};
const checkSort = ref(false);

const onFilter = (event) => {
  filterSQL.value = [];

  for (const [key, value] of Object.entries(event.filters)) {
    if (key != "global") {
      let obj = {
        key: key != "project_name" ? "project_name" : key,
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
watch(
  () => store.getters.langid,
  function () {
    loadData(true);
  }
);
//ADD log
const addLog = (log) => {
  axios.post(baseURL + "/api/Proc/AddLog", log, config);
};
//Sửa bản ghi
const editProject = (value) => {
  if (value.keyword != null && value.keyword.length > 1) {
    if (!Array.isArray(value.keyword)) {
      value.keyword = value.keyword.split(",");
    }
  }
  project.value = value;
  isChirlden.value = false;
  submitted.value = false;

  if (value.parent_id != null) {
    isChirlden.value = true;
    idParent.value = value.parent_id;
    datalists.value.forEach((element) => {
      if (element.project_id == value.parent_id) {
        nameParent.value = element.project_name;
      }
    });
    project.value.project_logo = value.project_logo;
  }
  headerDialog.value = "Cập nhật Menu";
  isSaveMenu.value = true;
  displayBasic.value = true;
};
//Xóa bản ghi
const delProject = (Menu, check) => {
  let listDel = [];
  let reDelChirl = (idDel) => {
    datalists.value.forEach((item) => {
      if (item.parent_id == idDel) {
        listDel.push(item.project_id);
        reDelChirl(item.project_id);
      }
    });
  };
  if (check) {
    listDel.push(Menu.project_id);
    reDelChirl(Menu.project_id);
  } else {
    for (const iterator of Menu) {
      listDel.push(iterator);
      reDelChirl(iterator);
    }
  }
  swal
    .fire({
      title: "Thông báo",
      text: "Bạn có muốn xoá dự án này không!",
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
          .delete(baseURL + "/api/api_project/Delete_api_project", {
            headers: { Authorization: `Bearer ${store.getters.token}` },
            data: listDel != null ? listDel : 1,
          })
          .then((response) => {
            swal.close();
            if (response.data.err != "1") {
              swal.close();
              toast.success("Xoá dự án thành công!");
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
const sttChirl = ref();
const nameParent = ref();
const idParent = ref();
//Thêm bản ghi con
const AddChirl = (value) => {
  nameParent.value = value.project_name;

  (async () => {
    await axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "api_project_count",
          par: [
            { par: "parent_id", va: value.project_id },
            { par: "search", va: options.value.SearchText },
          ],
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
          controller: "MenuView.vue",
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
    project.value = {
      project_name: "",
      status: null,
      is_order: sttChirl.value,
    };
    isChirlden.value = true;
    submitted.value = false;
    headerDialog.value = "Thêm Dự án Con";
    isSaveMenu.value = false;
    idParent.value = value.project_id;
    displayBasic.value = true;
  })();
};
//Tìm kiếm
const searchMenu = () => {
  options.value.loading = true;
  loadData(true);
};
//Thêm bản ghi
const isSaveMenu = ref(false);
const saveProject = (isFormValid) => {
  submitted.value = true;
  if (!isFormValid) {
    return;
  }
  let formData = new FormData();
  for (var i = 0; i < files.length; i++) {
    let file = files[i];
    formData.append("project_logo", file);
  }

  if (isChirlden.value) {
    project.value.parent_id = idParent.value;
  } else {
    project.value.parent_id = null;
  }
  if (project.value.keyword != null) {
    project.value.keyword = project.value.keyword.toString();
  }
  formData.append("project", JSON.stringify(project.value));
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  if (!isSaveMenu.value) {
    axios
      .post(baseURL + "/api/api_project/Add_api_project", formData, config)
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Thêm dự án thành công!");
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
      .put(baseURL + "/api/api_project/Update_api_project", formData, config)
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Sửa dự án thành công!");

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
const refreshProject = () => {
  options.value.loading = true;
  loadData(true);
};
const deleteList = () => {
  let listId = new Array(selectedNodes.value.length);
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

        selectedNodes.value.forEach((item) => {
          listId.push(item);
        });

        axios
          .delete(baseURL + "/api/api_project/Delete_api_project", {
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
    saveProject,
    isFirst,

    deleteList,
  };
});
</script>
<template>
  <div class="surface-100">
    <div class="h-2rem p-3 pb-0 m-3 mb-0 surface-0">
      <h3 class="m-0">
        <i class="pi pi-money-bill"></i> Danh sách dự án ({{
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
            type="text"
            spellcheck="false"
            placeholder="Tìm kiếm"
          />
        </span>
      </template>

      <template #end>
        <Button
          v-if="selectedNodes.length > 0"
          @click="deleteList()"
          label="Xóa"
          icon="pi pi-trash"
          class="mr-2 p-button-danger"
        />
        <Button
          @click="openBasic('Thêm dự án')"
          label="Thêm mới"
          icon="pi pi-plus"
          class="mr-2"
        />
        <Button
          @click="refreshProject"
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
          ref="projectButs"
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
        :paginator="options.totalRecords > options.PageSize"
        :rows="options.PageSize"
        style="margin-bottom: 2rem"
        :scrollable="true"
        scrollHeight="flex"
        v-model:selectionKeys="selectedKeys"
        :loading="options.loading"
        :expandedKeys="expandedKeys"
        @page="onPage($event)"
        @sort="onSort($event)"
        :rowHover="true"
        :showGridlines="true"
        responsiveLayout="scroll"
        selectionMode="checkbox"
      >
        <Column
          field="is_order"
          header="STT"
          :sortable="true"
          headerStyle="text-align:center;max-width:75px;height:50px"
          bodyStyle="text-align:center;max-width:75px;;max-height:60px"
          class="align-items-center justify-content-center text-center"
        >
          <template #body="project">
            <div
              v-if="project.node.data.parent_id == null"
              style="font-weight: 700"
            >
              {{ project.node.data.is_order }}
            </div>
            <div v-else style="font-size: 16px">
              {{ project.node.data.is_order }}
            </div>
          </template>
        </Column>

        <Column
          field="project_name"
          header="Tên dự án"
          :expander="true"
          :sortable="true"
          headerStyle="height:50px"
          bodyStyle="max-height:60px"
        >
        </Column>

        <Column
          field="db_name"
          header="Database"
          headerStyle="text-align:center;max-width:200px;height:50px"
          bodyStyle="text-align:center;max-width:200px;max-height:60px"
          class="align-items-center justify-content-center text-center"
        ></Column>
        <Column
          field="project_logo"
          header="Logo"
          headerStyle="text-align:center;max-width:200px;height:50px"
          bodyStyle="text-align:center;max-width:200px;max-height:60px"
          class="align-items-center justify-content-center text-center"
        >
          <template #body="data">
            <img
              style="object-fit: contain; border: unset; outline: unset"
              width="150"
              height="50"
              alt=" "
              v-bind:src="
                data.node.data.project_logo
                  ? basedomainURL + data.node.data.project_logo
                  : '/src/assets/image/fails.png'
              "
            /> </template
        ></Column>
        <Column
          field="status"
          header="Trạng thái"
          headerStyle="text-align:center;max-width:200px;height:50px"
          bodyStyle="text-align:center;max-width:200px;;max-height:60px"
          class="align-items-center justify-content-center text-center"
        >
          <template #body="data">
            <div v-if="data.node.data.status == 0">Đang lập kế hoạch</div>
            <div v-if="data.node.data.status == 1">Đang thực hiện</div>
            <div v-if="data.node.data.status == 2">Đã hoàn thành</div>
            <div v-if="data.node.data.status == 3">Tạm dừng</div>
            <div v-if="data.node.data.status == 4">Đóng</div>
          </template></Column
        >
        <Column
          header="Chức năng"
          class="align-items-center justify-content-center text-center"
          headerStyle="text-align:center;max-width:200px;height:50px"
          bodyStyle="text-align:center;max-width:200px;;max-height:60px"
        >
          <template #body="Project">
            <Button
              @click="AddChirl(Project.node.data)"
              class="p-button-rounded p-button-secondary p-button-outlined mx-1"
              type="button"
              icon="pi pi-plus-circle"
            ></Button>
            <Button
              @click="editProject(Project.node.data)"
              class="p-button-rounded p-button-secondary p-button-outlined mx-1"
              type="button"
              icon="pi pi-pencil"
            ></Button>
            <Button
              @click="delProject(Project.node.data, true)"
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
    :style="{ width: '50vw' }"
  >
    <form>
      <div class="grid formgrid m-2">
        <div class="col-12 field m-0 p-0">
          <div class="field col-12 md:col-12 flex" v-if="isChirlden">
            <div class="col-2 p-0">Cấp cha</div>
            <div class="col-10 p-0">
              <InputText
                v-model="nameParent"
                :disabled="true"
                class="col-10 ip36 px-2"
              />
            </div>
          </div>
          <div class="field col-12 md:col-12 flex">
            <div class="col-2 p-0">
              <label class="col-12 text-left px-0 m-0">
                Tên dự án <span class="redsao">(*)</span>
              </label>
            </div>
            <div class="col-10 p-0">
              <InputText
                v-model="project.project_name"
                spellcheck="false"
                class="col-12 ip36"
                :class="{
                  'p-invalid': v$.project_name.$invalid && submitted,
                }"
              />
            </div>
          </div>
          <div style="display: flex" class="field col-12 md:col-12">
            <div class="col-2 text-left"></div>
            <small
              v-if="
                (v$.project_name.$invalid && submitted) ||
                v$.project_name.$pending.$response
              "
              class="col-10 p-error"
            >
              <span class="col-12 p-0">{{
                v$.project_name.required.$message
                  .replace("Value", "Tên dự án")
                  .replace("is required", "không được để trống")
              }}</span>
            </small>
          </div>
          <div class="field col-12 md:col-12 flex">
            <div class="col-2 p-0">
              <label class="col-12 text-left px-0">
                Tên database <span class="redsao">(*)</span>
              </label>
            </div>
            <div class="col-10 p-0">
              <Dropdown
                class="col-12 ip36 p-0"
                v-model="project.db_name"
                :options="listDatabase"
                optionLabel="name"
                optionValue="code"
                placeholder="Chọn database"
                :class="{
                  'p-invalid': v$.db_name.$invalid && submitted,
                }"
              />
            </div>
          </div>
          <div style="display: flex" class="field col-12 md:col-12">
            <div class="col-2 text-left"></div>
            <small
              v-if="
                (v$.db_name.$invalid && submitted) ||
                v$.db_name.$pending.$response
              "
              class="col-10 p-error"
            >
              <span class="col-12 p-0">{{
                v$.db_name.required.$message
                  .replace("Value", "Tên database")
                  .replace("is required", "không được để trống")
              }}</span>
            </small>
          </div>
        </div>
        <div class="col-12 field flex">
          <label class="col-2 text-left p-0">Mô tả</label>

          <div class="col-10 mx-0 px-0">
            <ckeditor
              class="col-10"
              :editor="editor"
              :config="editorConfig"
              v-model="project.project_des"
              spellcheck="false"
            ></ckeditor>
          </div>
        </div>
        <div class="col-12 field md:col-12 p-0 flex">
          <div class="col-8 p-0">
            <div class="col-12 flex p-0">
              <div class="field col-6 md:col-6 p-0">
                <label class="col-6 text-left p-0">Số thứ tự </label>
                <InputNumber v-model="project.is_order" class="col-6 ip36" />
              </div>
              <div class="field col-6 md:col-6 p-0">
                <label class="col-4 text-center p-0">Trạng thái </label>
                <Dropdown
                  class="col-8 ip36 p-0"
                  v-model="project.status"
                  :options="trangThai"
                  optionLabel="name"
                  optionValue="code"
                  placeholder="Chọn trạng thái"
                  
                />
              </div>
            </div>
            <div class="col-12 field flex p-0">
              <label class="col-3 text-left p-0">Từ khóa </label>
              <Chips v-model="project.keyword" class="col-9 text-left pr-0" />
            </div>
          </div>

          <div class="col-4">
            <div class="field col-12 flex p-0 m-0">
              <label class="col-3 text-center p-0">Ảnh</label>
              <div class="inputanh col-12" @click="chonanh('AnhTem')">
                <img
                  id="logoTem"
                  v-bind:src="
                    project.project_logo
                      ? basedomainURL + project.project_logo
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
        @click="saveProject(!v$.$invalid)"
      />
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