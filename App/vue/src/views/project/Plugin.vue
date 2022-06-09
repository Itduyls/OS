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
  plugin_name: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.STARTS_WITH }],
  },
});
const rules = {
  plugin_name: {
    required,
    $errors: [
      {
        $property: "plugin_name",
        $validator: "required",
        $message: "Tên thư viện không được để trống!",
      },
    ],
  },
};
const plugin = ref({
  plugin_name: "",
  images: "",
  status: true,
  is_app: false,
  is_order: 1,
  des: "",
  is_url: "",
  keywords: null,
});
const category = ref({
  category_name: "",
  is_order: 1,
  status: true,
});

const rulesCate = {
  category_name: {
    required,
    $errors: [
      {
        $property: "category_name",
        $validator: "required",
        $message: "Tên loại không được để trống!",
      },
    ],
  },
};
const isSaveCategory = ref(false);
const listImg = ref([]);
const showThumbnails = ref(false);
const selectedPlugins = ref();
const submitted = ref(false);
const v$ = useVuelidate(rules, plugin);
const validatePlugin = useVuelidate(rulesCate, category);
const isSavePlugin = ref(false);
const datalists = ref();
const toast = useToast();
const basedomainURL = baseURL;
const checkDelList = ref(false);
const listThumbnails = ref([]);
const options = ref({
  IsNext: true,
  sort: "plugin_id",
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
        proc: "api_plugin_count",
        par: [{ par: "search", va: options.value.SearchText }],
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data)[0];
      if (data.length > 0) {
        options.value.totalRecords = data[0].totalRecords;

        sttPlugin.value = data[0].totalRecords + 1;
      }
    })
    .catch((error) => {
      addLog({
        title: "Lỗi Console loadCount",
        controller: "Plugin.vue",
        log_content: error.message,
      });
    });
};
//Lấy dữ liệu thư viện
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
    // axios
    //   .post(
    //     baseURL + "/api/Proc/CallProc",
    //     {
    //       proc: "api_plugin_list",
    //       par: [
    //         { par: "pageno", va: options.value.PageNo },
    //         { par: "pagesize", va: options.value.PageSize },
    //         { par: "search", va: options.value.SearchText },
    //         { par: "status", va: options.value.Status },
    //       ],
    //     },
    //     config
    //   )
    //   .then((response) => {
    //     let data = JSON.parse(response.data.data)[0];
    //     if (isFirst.value) isFirst.value = false;
    //     data.forEach((element) => {
    //       if (element.keywords != null && element.keywords.length > 1) {
    //         if (!Array.isArray(element.keywords)) {
    //           element.keywords = element.keywords.split(",");
    //         }
    //       }
    //       let arrImg = [];
    //       if (element.images != null && element.images.length > 1) {
    //         if (!Array.isArray(element.images)) {
    //           let arr = element.images.split(",");

    //           arr.forEach((element, i) => {
    //             arrImg.push({
    //               id: i + 1,
    //               itemImageSrc: basedomainURL + element,
    //               thumbnailImageSrc: basedomainURL + element,
    //               alt: element,
    //               title: basedomainURL + element,
    //             });
    //           });
    //         }
    //         element.images = arrImg;
    //       }
    //     });
    //     datalists.value = data;
    //     options.value.loading = false;
    //   })
    //   .catch((error) => {
    //     console.log("rrr", error);
    //     toast.error("Tải dữ liệu không thành công!");
    //     options.value.loading = false;
    //     addLog({
    //       title: "Lỗi Console loadData",
    //       controller: "TemView.vue",
    //       logcontent: error.message,
    //       loai: 2,
    //     });
    //     if (error && error.status === 401) {
    //       swal.fire({
    //         title: "Error!",
    //         text: "Mã token đã hết hạn hoặc không hợp lệ, vui lòng đăng nhập lại!",
    //         icon: "error",
    //         confirmButtonText: "OK",
    //       });
    //       store.commit("gologout");
    //     }
    //   });
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

    options.value.id = datalists.value[datalists.value.length - 1].plugin_id;
    options.value.IsNext = true;
  } else if (event.page < options.value.PageNo) {
    //Trang trước
    options.value.id = datalists.value[0].plugin_id;
    options.value.IsNext = false;
  }
  options.value.PageNo = event.page;
  loadData(true);
};
//Hiển thị dialog
const headerDialog = ref();
const displayBasic = ref(false);
const openBasic = (str) => {
  files = [];
  submitted.value = false;
  checkImage.value = true;
  showThumbnails.value = false;
  plugin.value = {
    plugin_name: "",
    images: "",
    status: true,
    is_default: false,
    is_order: sttPlugin.value,
    des: "",
    is_url: "",
    keywords: null,
  };
  listThumbnails.value = [
    {
      id: 1,
      itemImageSrc: "/src/assets/image/noimg.jpg",
      thumbnailImageSrc: "/src/assets/image/noimg.jpg",
      alt: "Description for Image 1",
      title: "Title 1",
    },
  ];
  checkIsmain.value = false;
  isSavePlugin.value = false;
  headerDialog.value = str;
  displayBasic.value = true;
};

const closeCategory = () => {
  category.value = ref({
    category_name: "",
    is_order: 1,
    status: true,
  });
  displayBasic.value = false;
};
const closeDialog = () => {
  plugin.value = {
    plugin_name: "",
    images: "",
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
const checkImage = ref(false);
const handleFileUpload = (event) => {
  for (let index = 0; index < event.target.files.length; index++) {
    files.push(event.target.files[index]);
  }
  if (files.length == 0) {
    return;
  }
  if (checkImage.value == true) {
    listThumbnails.value = [];
    checkImage.value = false;
  }
  let id = listThumbnails.value.length;
  var allowedExtensions = /(\.jpg|\.jpeg|\.png|\.gif)$/i;
  if (files.length > 0) {
    for (let index = 0; index < files.length; index++) {
      if (allowedExtensions.exec(files[index].name)) {
        let image = ref();
        id += 1;
        listImg.value.push(files[index]);
        image.id = id;
        image.itemImageSrc = URL.createObjectURL(files[index]);
        image.thumbnailImageSrc = URL.createObjectURL(files[index]);
        image.alt = files[index].name + index;
        image.title = files[index].name + index;
        listThumbnails.value.push(image);
      }
    }
  }
  showThumbnails.value = true;
};
//Thêm bản ghi
let files = [];
const sttPlugin = ref(1);
const saveData = (isFormValid) => {
  submitted.value = true;
  if (!isFormValid) {
    return;
  }
  let formData = new FormData();
  for (var i = 0; i < files.length; i++) {
    let file = files[i];
    formData.append("url", file);
  }
  if (plugin.value.keywords != null) {
    plugin.value.keywords = plugin.value.keywords.toString();
  }
  formData.append("plugin", JSON.stringify(plugin.value));
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  if (!isSavePlugin.value) {
    axios
      .post(baseURL + "/api/api_plugin/Add_plugin", formData, config)
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Thêm thư viện thành công!");
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
      .put(baseURL + "/api/api_plugin/Update_plugin", formData, config)
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Sửa thư viện thành công!");
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
const projectSelected = ref();
const listCategorySave = ref([]);
const listProject = ref([]);
const checkIsmain = ref(true);
//Sửa bản ghi
const editPlugin = (dataTem) => {
  submitted.value = false;
  showThumbnails.value = true;
  listThumbnails.value = dataTem.images;
  console.log("Dữ lêiuj", dataTem.images);
  // let imgs="";
  // let detached='';
  // for (let index = 0; index < dataTem.images.length; index++) {

  //  imgs+=detached+dataTem.images[index].alt;
  //  detached=",";
  // }
  // dataTem.images=imgs;
  plugin.value = dataTem;

  headerDialog.value = "Sửa thư viện";
  isSavePlugin.value = true;
  displayBasic.value = true;
};
//Xóa bản ghi
const delTem = (Tem) => {
  swal
    .fire({
      title: "Thông báo",
      text: "Bạn có muốn xoá thư viện này không!",
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
          toast.error("Không được xóa thư viện chính!");
          swal.close();
          return;
        } else {
          axios
            .delete(baseURL + "/api/api_plugin/Delete_plugin", {
              headers: { Authorization: `Bearer ${store.getters.token}` },
              data: Tem != null ? [Tem.plugin_id] : 1,
            })
            .then((response) => {
              swal.close();
              if (response.data.err != "1") {
                swal.close();
                toast.success("Xoá thư viện thành công!");
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
        excelname: "DANH SÁCH TEM",
        proc: "api_plugin_listexport",
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
    .post(baseURL + "/api/SQL/Filter_Plugin", data, config)
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
const searchPlugin = (event) => {
  if (event.code == "Enter") {
    options.value.loading = true;
    loadData(true);
  }
};
const refreshPlugin = () => {
  options.value.loading = true;
  loadData(true);
};
const onFilter = (event) => {
  filterSQL.value = [];

  for (const [key, value] of Object.entries(event.filters)) {
    if (key != "global") {
      let obj = {
        key: key != "plugin_name" ? "plugin_name" : key,
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
      IntID: value.plugin_id,
      TextID: value.plugin_id + "",
      IntTrangthai: 1,
      BitTrangthai: value.status,
    };
    axios
      .put(baseURL + "/api/api_plugin/Update_TrangthaiPlugin", data, config)
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Sửa trạng thái thư viện thành công!");
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
      IntID: value.plugin_id,
      TextID: value.plugin_id + "",
      BitMain: value.is_default,
    };
    axios
      .put(baseURL + "/api/api_plugin/Update_DefaultPlugin", data1, config)
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Sửa trạng thái thư viện thành công!");
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
  let listId = new Array(selectedPlugins.length);
  let checkD = false;
  selectedPlugins.value.forEach((item) => {
    if (item.is_default) {
      toast.error("Không được xóa thư viện mặc định!");
      checkD = true;
      return;
    }
  });
  if (!checkD) {
    swal
      .fire({
        title: "Thông báo",
        text: "Bạn có muốn xoá thư viện này không!",
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

          selectedPlugins.value.forEach((item) => {
            listId.push(item.plugin_id);
          });
          axios
            .delete(baseURL + "/api/api_plugin/Delete_plugin", {
              headers: { Authorization: `Bearer ${store.getters.token}` },
              data: listId != null ? listId : 1,
            })
            .then((response) => {
              swal.close();
              if (response.data.err != "1") {
                swal.close();
                toast.success("Xoá thư viện thành công!");
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

watch(selectedPlugins, () => {
  if (selectedPlugins.value.length > 0) {
    checkDelList.value = true;
  } else {
    checkDelList.value = false;
  }
});
const layout = ref("grid");
const checkEditPlugin = ref();
const toggleMores = (event, u) => {
  plugin.value = u;

  menuButMores.value.toggle(event);
};
const menuButMores = ref();
const itemButMores = ref([
  {
    label: "Sửa",
    icon: "pi pi-cog",
    command: (event) => {
      editPlugin(plugin.value);
    },
  },

  {
    label: "Xoá",
    icon: "pi pi-trash",
    command: (event) => {
      if (checkEditPlugin.value) {
        deleteCategory(plugin.value.plugin_id);
      } else {
        deleteService(plugin.value.plugin_id);
      }
    },
  },
]);
const onUploadFile = (event) => {
  event.files.forEach((element) => {
    files.push(element);
  });
};
const loadPlugin = () => {
  console.log("Category", projectSelected.value);
};
const listCategory = ref([]);
const database_name = ref();
const projectLogo = ref();

const renderService = (listCate, listPlug) => {
  let arrChils = [];
  listCate
    .filter((x) => x.parent_id == null)
    .forEach((m) => {
      let om = { key: m.category_id, data: m };
      const rechildren = (mm, category_id) => {
        if (!mm.children) mm.children = [];
        let dts = listCate.filter((x) => x.parent_id == category_id);
        if (dts.length > 0) {
          dts.forEach((em) => {
            let om1 = { key: em.category_id, data: em };
            rechildren(om1, em.category_id);
            mm.children.push(om1);
          });
        }
        if (listPlug.length > 0) {
          let dsv = listPlug.filter((x) => x.category_id == category_id);
          if (dsv.length > 0) {
            dsv.forEach((em) => {
              let om1 = { key: em.plugin_name, data: em };
              mm.children.push(om1);
            });
          }
        }
      };
      rechildren(om, m.category_id);
      arrChils.push(om);
    });
  console.log("aiza", arrChils);
  //   arrtreeChils.unshift({ key: -1, data: -1, label: "-----Chọn Module----" });
  listCategory.value = arrChils;
};
const loadProject = () => {
  (async () => {
    listProject.value = [];
    await axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "api_project_list_api",
          par: [
            { par: "search", va: options.value.SearchText },
            { par: "status", va: options.value.Status },
          ],
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];
        projectSelected.value = data[0].project_id;
        database_name.value = data[0].db_name;
        projectLogo.value = data[0].project_logo;
        data.forEach((element) => {
          let db = {
            name: element.project_name,
            code: element.project_id,
            db_name: element.db_name,
            project_logo: element.project_logo,
          };
          listProject.value.push(db);
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

    listCategory.value = [];
    listCategorySave.value = [];
    let listCate = [];
    let listPlug = [];
    await axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "api_category_list",
          par: [
            { par: "parent_id", va: options.value.parent_id },
            { par: "project_id", va: projectSelected.value },
            { par: "search", va: options.value.SearchText },
            { par: "status", va: options.value.Status },
          ],
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];
        listCate = data;
        listCategorySave.value = data;
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
          proc: "api_plugin_list_all",
          par: [
            { par: "search", va: options.value.SearchText },
            { par: "status", va: options.value.Status },
          ],
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];
        if (isFirst.value) isFirst.value = false;

        renderService(listCate, data);
        options.value.loading = false;
      })
      .catch((error) => {
        console.log(error);
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
const isChirlden = ref(false);
const categoryIdSave = ref();
const selectedKey = ref();
const expandedKeys = ref({});
const nodeValue = ref();
const categoryName = ref();
const checkNode = ref(false);
const onNodeSelect = (node) => {
  if (expandedKeys.value[node.key] == true) {
    expandedKeys.value[node.key] = false;
  } else {
    expandedKeys.value[node.key] = true;
  }

  checkNode.value = true;
  nodeValue.value = node;
  options.value.loading = true;
  categoryName.value = node.data.category_name;
  if (node.data.category_id == categoryIdSave.value) {
    return;
  } else {
    isTypeAPI.value = true;
    nodeSelected.value = node.data;
    datalists.value = [];
    categoryIdSave.value = node.data.category_id;
    (async () => {
      await axios
        .post(
          baseURL + "/api/Proc/CallProc",
          {
            proc: "api_category_list",
            par: [
              { par: "parent_id", va: node.data.category_id },
              { par: "project_id", va: projectSelected.value },
              { par: "search", va: options.value.SearchText },
              { par: "status", va: options.value.Status },
            ],
          },
          config
        )
        .then((response) => {
          let data = JSON.parse(response.data.data)[0];
          data.forEach((element) => {
            datalists.value.push(element);
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

      await axios
        .post(
          baseURL + "/api/Proc/CallProc",
          {
            proc: "api_plugin_list",
            par: [
              { par: "category_id", va: node.data.category_id },
              { par: "search", va: options.value.SearchText },
              { par: "status", va: options.value.Status },
            ],
          },
          config
        )
        .then((response) => {
          console.log(response);
          let data = JSON.parse(response.data.data)[0];
          if (data.lenght > 0) {
            data.forEach((element) => {
              datalists.value.push(element);
            });
          }

          options.value.loading = false;
        })
        .catch((error) => {
          toast.error("Tải dữ liệu không thành công!");
          options.value.loading = false;
          console.log(error);
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
      options.value.totalRecords = datalists.value.length;
    })();
  }
};
const onUnNodeSelect = (node) => {};
const isTypeAPI = ref(true);

const loadCategory = () => {
  let listCate = [];
  let listPlug = [];
  (async () => {
    listCategorySave.value = [];
    await axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "api_category_list",
          par: [
            { par: "parent_id", va: options.value.parent_id },
            { par: "project_id", va: projectSelected.value },
            { par: "search", va: options.value.SearchText },
            { par: "status", va: options.value.Status },
          ],
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];
        listCategorySave.value = data;
        listCate = data;
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
          proc: "api_plugin_list_all",
          par: [
            { par: "search", va: options.value.SearchText },
            { par: "status", va: options.value.Status },
          ],
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];
        if (isFirst.value) isFirst.value = false;

        renderService(listCate, data);
        options.value.loading = false;
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
const refreshTypeApi = () => {
  options.value.loading = true;
  loadProject();
  onNodeSelect(nodeValue.value);
};
const nodeSelected = ref();
const headerCate = ref();
const displayCate = ref();
const addCategory = (str) => {
  submitted.value = false;
  headerCate.value = str;
  let sttCate = listCategory.value.length + 1;
  if (nodeSelected.value) {
    let stt = 0;
    listCategorySave.value.forEach((element) => {
      if (element.parent_id == nodeSelected.value.category_id) {
        stt++;
      }
    });
    sttCate = stt + 1;
  }
  category.value = {
    category_name: "",
    is_order: sttCate,
    status: true,
    parent_id:
      nodeSelected.value != null ? nodeSelected.value.category_id : null,
    project_id: projectSelected.value,
  };
  isChirlden.value = false;
  if (category.value.parent_id != null) {
    listCategorySave.value.forEach((element) => {
      if (element.category_id == category.value.parent_id) {
        nameParent.value = element.category_name;
        isChirlden.value = true;
        return;
      }
    });
  }
  isSaveCategory.value = false;
  displayCate.value = true;
};

const saveCategory = (isFormValid) => {
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
  if (!isSaveCategory.value) {
    axios
      .post(baseURL + "/api/api_category/Add_category", category.value, config)
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Thêm loại API thành công!");
          loadProject();
          closeCategory();
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
      .put(
        baseURL + "/api/api_category/Update_category",
        category.value,
        config
      )
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Sửa loại API thành công!");

          loadProject();
          closeCategory();
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
onMounted(() => {
  store.commit("setisadmin", true);
  loadData(true);
  loadProject();
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
    searchPlugin,
    onCheckBox,
    selectedPlugins,
    deleteList,
  };
});
</script>
<template>
  <div class="surface-100">
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
            v-model="projectSelected"
            :options="listProject"
            optionLabel="name"
            optionValue="code"
            placeholder="Chọn dự án"
            class="w-full"
            @change="loadCategory"
          >
          </Dropdown>
          <Button
            class="w-4rem ml-2 p-button-outlined p-button-secondary"
            icon="pi pi-refresh"
            @click="refreshTypeApi"
          />
        </div>

        <div style="height: calc(100vh - 128px)">
          <TreeTable
            :value="listCategory"
            @nodeSelect="onNodeSelect"
            @node-unselect="onUnNodeSelect"
            selectionMode="single"
            v-model:selectionKeys="selectedKey"
            class="h-full w-full overflow-x-hidden"
            scrollHeight="flex"
            responsiveLayout="scroll"
            :scrollable="true"
            :expandedKeys="expandedKeys"
          >
            <Column
              field="category_name"
              :expander="true"
              class="cursor-pointer flex"
            >
              <template #header>
                <Toolbar class="w-full p-0 border-none sticky top-0">
                  <template #start>
                    <div class="font-bold text-xl">Loại thư viện</div>
                  </template>
                  <template #end>
                    <div v-if="isTypeAPI">
                      <Button
                        icon="pi pi-plus "
                        class="p-button-success"
                        @click="addCategory('Thêm loại API')"
                      />
                      <Button
                        class="mx-1"
                        v-if="nodeSelected != null"
                        type="button"
                        icon="pi pi-pencil"
                        @click="editCategory(nodeSelected.category_id)"
                      ></Button>
                      <Button
                        icon="pi pi-trash"
                        class="p-button-danger"
                        v-if="nodeSelected != null"
                        @click="deleteCategory(nodeSelected.category_id)"
                      />
                    </div>
                  </template>
                </Toolbar>
              </template>
              <template #body="data">
                <div
                  class="relative flex w-full p-0"
                  v-if="!data.node.data.plugin_id"
                >
                  <div class="grid w-full p-0">
                    <div
                      class="field col-12 md:col-12 w-full flex m-0 p-0 pt-2"
                    >
                      <div class="col-2 p-0">
                        <img
                          src="../../assets/image/folder.png"
                          width="28"
                          height="36"
                          style="object-fit: contain"
                        />
                      </div>
                      <div class="col-10 p-0">
                        <div
                          class="px-2"
                          style="line-height: 30px; word-break: break-all"
                        >
                          {{ data.node.data.category_name }}
                          <span v-if="data.node.children.length > 0"
                            >({{ data.node.children.length }})</span
                          >
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
                <div class="relative flex w-full p-0" v-else>
                  <div class="grid w-full p-0">
                    <div
                      class="field col-12 md:col-12 w-full flex m-0 p-0 pt-2"
                    >
                      <div class="col-2 p-0">
                        <img
                          src="../../assets/image/service.png"
                          class="pr-2 pb-0"
                          width="28"
                          height="36"
                          style="object-fit: contain"
                        />
                      </div>
                      <div class="col-9 p-0">
                        <div style="line-height: 30px; word-break: break-all">
                          {{ data.node.data.plugin_name }}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </template>
            </Column>
          </TreeTable>
        </div>
      </SplitterPanel>
      <SplitterPanel :size="80">
        <div class="d-lang-table">
          <DataView
            class="w-full h-full e-sm flex flex-column"
            paginatorTemplate="FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink CurrentPageReport RowsPerPageDropdown"
            :rowsPerPageOptions="[8, 12, 20, 50, 100]"
            currentPageReportTemplate=""
            responsiveLayout="scroll"
            :scrollable="true"
            :layout="layout"
            :lazy="true"
            :value="datalists"
            :loading="options.loading"
            :paginator="options.totalRecords > options.PageSize"
            :rows="options.PageSize"
            :totalRecords="options.totalRecords"
          >
            <template #header>
              <div>
                <h3 class="m-0">
                  <i class="pi pi-book"></i> Danh sách thư viện
                  <span v-if="options.totalRecords > 0"
                    >({{ options.totalRecords }})</span
                  >
                </h3>
                <Toolbar class="w-full custoolbar pt-5">
                  <template #start>
                    <span class="p-input-icon-left mr-2">
                      <i class="pi pi-search" />
                      <InputText
                        type="text"
                        class="p-inputtext-sm"
                        spellcheck="false"
                        placeholder="Tìm kiếm"
                      />
                    </span>
                    <Dropdown
                      v-model="projectSelected"
                      :options="listProject"
                      optionLabel="name"
                      optionValue="code"
                      placeholder="Chọn dự án"
                      class="w-full"
                      @change="loadPlugin"
                    >
                    </Dropdown>
                  </template>

                  <template #end>
                    <DataViewLayoutOptions v-model="layout" class="mr-2" />

                    <Button
                      @click="openBasic('Thêm thư viện')"
                      label="Thêm mới"
                      icon="pi pi-plus"
                      class="p-button-sm mr-2"
                    />
                    <Button
                      class="
                        mr-2
                        p-button-sm p-button-outlined p-button-secondary
                      "
                      icon="pi pi-refresh"
                    />
                    <Button
                      label="Tiện ích"
                      icon="pi pi-file-excel"
                      class="mr-2 p-button-outlined p-button-secondary"
                      aria-haspopup="true"
                      aria-controls="overlay_Export"
                    />
                    <Menu id="overlay_Export" ref="projectButs" :popup="true" />
                  </template>
                </Toolbar>
              </div>
            </template>
            <template #grid="slotProps">
              <div class="col-12 md:col-3 p-2">
                <Card class="no-paddcontent">
                  <template #title>
                    <div style="position: relative">
                      <div>
                        <div
                          class="
                            align-items-center
                            justify-content-center
                            text-center
                          "
                        >
                          <Avatar
                            image="./src/assets/image/paramester.png"
                            class="mr-2"
                            size="xlarge"
                            shape="circle"
                          />
                        </div>
                      </div>
                      <Button
                        style="position: absolute; right: 0px; top: 0px"
                        icon="pi pi-ellipsis-h"
                        class="p-button-rounded p-button-text ml-2"
                        @click="toggleMores($event, slotProps.data)"
                        aria-haspopup="true"
                        aria-controls="overlay_More"
                      />
                      <Menu
                        id="overlay_More"
                        ref="menuButMores"
                        :model="itemButMores"
                        :popup="true"
                      />
                    </div>
                  </template>
                  <template #subtitle>
                    <div>
                      <div>
                        <i
                          v-if="slotProps.data.is_app"
                          class="pi pi-mobile"
                        ></i>
                        <i
                          v-else
                          style="color: transparent"
                          class="pi pi-mobile"
                        ></i>
                      </div>
                    </div>
                  </template>
                  <template #content>
                    <div class="text-center">
                      <div>
                        <div class="text-lg text-blue-400 font-bold pb-2">
                          {{ slotProps.data.plugin_name }}
                        </div>
                        <div>
                          {{ slotProps.data.is_url }}
                        </div>
                      </div>
                    </div>
                  </template>
                </Card>
              </div>
            </template>
            <template #list="slotProps">
              <div class="w-full">
                <div class="flex align-items-center justify-content-center">
                  <div
                    class="
                      flex flex-column flex-grow-1
                      surface-0
                      m-2
                      border-round-xs
                    "
                  >
                    <div class="col-12 field flex p-0 m-0 px-2">
                      <div class="col-8 p-0">
                        <div class="col-12 p-0 font-bold text-xl">
                          <div>{{ slotProps.data.plugin_name }}</div>
                        </div>
                        <div class="col-12 p-0">
                          <small class="pr-3">
                            <i class="pi pi-user text-color-secondary"></i>
                            {{ slotProps.data.created_by }}</small
                          >

                          <small>
                            <i class="pi pi-calendar text-color-secondary"></i>
                            {{
                              moment(
                                new Date(slotProps.data.created_date)
                              ).format("DD/MM/YYYY")
                            }}</small
                          >
                        </div>
                      </div>
                      <div class="col-4 text-right flex">
                        <Toolbar
                          class="w-full surface-0 outline-none border-none p-0"
                        >
                          <template #start>
                            <div class="flex">
                              <div>
                                <div v-if="slotProps.data.status">
                                  <Button
                                    class="p-button-rounded p-button-secondary"
                                    >Kích hoạt</Button
                                  >
                                </div>
                                <div v-else>
                                  <Button
                                    class="
                                      p-button-rounded p-button-secondary
                                      ml-3
                                    "
                                    >Khóa</Button
                                  >
                                </div>
                              </div>
                              <div class="pl-8">
                                <Galleria
                                  :showThumbnails="false"
                                  :value="slotProps.data.images"
                                  :numVisible="3"
                                  :showIndicators="true"
                                >
                                  <template #item="slotProps">
                                    <img
                                      :src="
                                        slotProps.item.thumbnailImageSrc
                                          ? slotProps.item.thumbnailImageSrc
                                          : '/src/assets/image/noimg.jpg'
                                      "
                                      style="
                                        background-color: #eeeeee;
                                        object-fit: contain;
                                        width: 150px;
                                        height: 100px;
                                      "
                                    />
                                  </template>
                                </Galleria>
                              </div>
                            </div>
                          </template>
                          <template #end>
                            <div>
                              <Button
                                icon="pi pi-ellipsis-h"
                                class="
                                  p-button-outlined p-button-secondary
                                  ml-2
                                  border-none
                                "
                                @click="toggleMores($event, slotProps.data)"
                                aria-haspopup="true"
                                aria-controls="overlay_More"
                              />
                              <Menu
                                id="overlay_More"
                                ref="menuButMores"
                                :model="itemButMores"
                                :popup="true"
                              />
                            </div>
                          </template>
                        </Toolbar>
                      </div>
                    </div>
                    <div class="col-12 field flex p-0 m-0 px-2 pb-2">
                      <div v-html="slotProps.data.des"></div>
                    </div>
                    <div class="col-12 field flex p-0 m-0 px-2 pb-2">
                      <div v-for="item in slotProps.data.keywords" :key="item">
                        <Chip :label="item" class="mr-2 mb-2" />
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </template>
            <template #empty>
              <div
                class="
                  align-items-center
                  justify-content-center
                  p-4
                  text-center
                "
                v-if="!isFirst"
              >
                <img src="../../assets/background/nodata.png" height="144" />
                <h3 class="m-1">Không có dữ liệu</h3>
              </div>
            </template>
          </DataView>
        </div>
      </SplitterPanel>
    </Splitter>
  </div>
  <Dialog
    :header="headerDialog"
    v-model:visible="displayBasic"
    :style="{ width: '50vw' }"
  >
    <form>
      <div class="grid formgrid m-2">
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left"
            >Thư viện <span class="redsao">(*)</span></label
          >
          <InputText
            v-model="plugin.plugin_name"
            spellcheck="false"
            class="col-10 ip36 px-2"
            :class="{ 'p-invalid': v$.plugin_name.$invalid && submitted }"
          />
        </div>
        <div class="field col-12 md:col-12 flex">
          <div class="col-3 text-left"></div>
          <small
            v-if="
              (v$.plugin_name.$invalid && submitted) ||
              v$.plugin_name.$pending.$response
            "
            class="col-9 p-error"
          >
            <span class="col-12 p-0">{{
              v$.plugin_name.required.$message
                .replace("Value", "Tên thư viện")
                .replace("is required", "không được để trống")
            }}</span>
          </small>
        </div>
        <div class="field col-8 md:col-8">
          <div class="col-12 p-0 flex field">
            <label class="col-3 text-left">Link</label>
            <InputText
              v-model="plugin.is_url"
              spellcheck="false"
              class="col-9 ip36 px-2"
            />
          </div>
          <div class="col-12 field md:col-12 p-0">
            <label class="col-3 text-left">Loại API</label>
            <Dropdown
              v-model="plugin.category_id"
              :options="listCategorySave"
              optionLabel="name"
              optionValue="code"
              placeholder="Chọn loại API"
              :editable="true"
              :filter="true"
              class="col-9 ip36 p-0"
            >
            </Dropdown>
          </div>
          <div class="field col-12 md:col-12 flex p-0">
            <label class="col-3 text-left pt-2">Mô tả</label>
            <div class="col-9 p-0">
              <!-- <Textarea v-model="plugin.des" class="col-12 ip36 p-0 m-0 "  rows="6" cols="30" autoResize /> -->

              <Editor v-model="plugin.des" editorStyle="height: 120px" />
            </div>
          </div>
        </div>
        <div class="field col-4 md:col-4">
          <label class="col-12 text-center p-0 pl-3">Hình ảnh</label>
          <!-- inputanh -->
          <div
            class="border-500 border-1 border-solid col-12 p-0 h-16rem mt-1"
            style="background-color: #eeeeee"
          >
            <Galleria
              :showThumbnails="showThumbnails"
              :value="listThumbnails"
              :numVisible="3"
            >
              <template #item="slotProps">
                <img
                  @click="chonanh('AnhTem')"
                  :src="
                    slotProps.item.thumbnailImageSrc
                      ? slotProps.item.thumbnailImageSrc
                      : '/src/assets/image/noimg.jpg'
                  "
                  style="background-color: #eeeeee; object-fit: contain"
                  class="w-full h-12rem"
                />
              </template>
              <template #thumbnail="slotProps">
                <img
                  :src="
                    slotProps.item.thumbnailImageSrc
                      ? slotProps.item.thumbnailImageSrc
                      : '/src/assets/image/noimg.jpg'
                  "
                  :alt="slotProps.item.alt"
                  style="object-fit: contain; width: 100%; height: 50px"
                />
              </template>
            </Galleria>
          </div>
          <input
            class="ipnone"
            id="AnhTem"
            type="file"
            multiple="true"
            accept="image/*"
            @change="handleFileUpload"
          />
        </div>

        <div class="col-12 p-0 flex field">
          <label class="col-2 text-left">File</label>
          <div class="col-10 p-0">
            <FileUpload
              chooseLabel="Chọn File"
              :showUploadButton="false"
              :showCancelButton="false"
              :multiple="true"
              accept=".zip,.rar"
              :maxFileSize="10000000"
              @select="onUploadFile"
            />
          </div>
        </div>
        <div class="field col-12 md:col-12 p-0 flex">
          <div class="col-4 field md:col-4 p-0">
            <label class="col-6 text-left">STT</label>
            <InputNumber v-model="plugin.is_order" class="col-6 ip36 p-0" />
          </div>
          <div class="col-4 flex pt-1">
            <label style="vertical-align: text-bottom" class="col-6 text-center"
              >Trạng thái
            </label>
            <InputSwitch v-model="plugin.status" class="col-6 ml-1" />
          </div>

          <div class="col-4 p-0 flex pt-1">
            <label style="vertical-align: text-bottom" class="col-5 text-center"
              >App
            </label>
            <InputSwitch v-model="plugin.is_app" class="col-6" />
          </div>
        </div>
        <div class="col-6 flex p-0"></div>

        <div class="field col-12 md:col-12 p-0">
          <label class="col-2 text-left">Từ khóa</label>
          <Chips
            v-model="plugin.keywords"
            spellcheck="false"
            class="col-10 ip36 p-0"
          />
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
  <Dialog
    :header="headerCate"
    v-model:visible="displayCate"
    :style="{ width: '40vw' }"
  >
    <form>
      <div class="grid formgrid m-2">
        <div v-if="isChirlden" class="field col-12 md:col-12 pb-2">
          <label class="col-3 text-left p-0"
            >Cấp cha<span class="redsao"></span
          ></label>
          <InputText
            spellcheck="false"
            v-model="nameParent"
            :disabled="true"
            class="col-8 ip36 px-2"
          />
        </div>

        <div class="field col-12 md:col-12">
          <label class="col-3 text-left p-0"
            >Tên loại Api <span class="redsao">(*)</span></label
          >
          <InputText
            v-model="category.category_name"
            spellcheck="false"
            class="col-8 ip36 px-2"
            :class="{
              'p-invalid': validatePlugin.category_name.$invalid && submitted,
            }"
          />
        </div>
        <div style="display: flex" class="field col-12 md:col-12">
          <div class="col-3 text-left"></div>
          <small
            v-if="
              (validatePlugin.category_name.$invalid && submitted) ||
              validatePlugin.category_name.$pending.$response
            "
            class="col-8 p-error p-0"
          >
            <span class="col-12 p-0">{{
              validatePlugin.category_name.required.$message
                .replace("Value", "Tên loại API")
                .replace("is required", "không được để trống")
            }}</span>
          </small>
        </div>
        <div style="display: flex" class="col-12 field md:col-12">
          <div class="field col-6 md:col-6 p-0">
            <label class="col-6 text-left p-0">Số thứ tự </label>
            <InputNumber v-model="category.is_order" class="col-6 ip36 p-0" />
          </div>
          <div class="field col-6 md:col-6 p-0">
            <label
              style="vertical-align: text-bottom"
              class="col-6 text-center p-0"
              >Trạng thái
            </label>
            <InputSwitch v-model="category.status" class="col-6" />
          </div>
        </div>
      </div>
    </form>
    <template #footer>
      <Button
        label="Hủy"
        icon="pi pi-times"
        @click="closeCategory"
        class="p-button-text"
      />

      <Button
        label="Lưu"
        icon="pi pi-check"
        @click="saveCategory(!validatePlugin.$invalid)"
      />
    </template>
  </Dialog>
</template>

<style scoped>
.d-lang-table {
  margin: 0px 8px 0px 8px;
  height: calc(100vh - 167px);
}
.inputanh {
  border: 1px solid #ccc;
  width: 224px;
  height: 128px;
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
<style lang="scss" scoped>
::v-deep(.p-galleria-content) {
  .p-galleria-item-wrapper {
    height: 100%;
  }
  .p-galleria-thumbnail-container {
    padding: 4px 2px;
    background-color: rgb(195, 195, 195);
  }
  .p-galleria-thumbnail-next {
    display: block;
  }
  .p-galleria-thumbnail-prev {
    display: block;
  }
}
</style>