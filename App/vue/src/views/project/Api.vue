<script setup>
import { ref, inject, onMounted, watch } from "vue";
import { useToast } from "vue-toastification";
import { required } from "@vuelidate/validators";
import { useVuelidate } from "@vuelidate/core";
import moment from "moment";
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
const filters = ref();
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

const listProject = ref([]);
const projectSelected = ref();
const listCategory = ref([]);
const database_name = ref();
watch(projectSelected, () => {
  listProject.value.forEach((element) => {
    if (element.code == projectSelected.value) {
      projectLogo.value = element.project_logo;
      database_name.value = element.db_name;
    }
  });
});
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
    let listSer = [];
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
          proc: "api_service_list_all",
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

        listSer = data;
        listService.value = data;
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
    await axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "api_parameter_list_all",
          par: [
            { par: "search", va: options.value.SearchText },
            { par: "status", va: options.value.Status },
          ],
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];
        listParameters.value = data;
        renderService(listCate, listSer, data);
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
    await axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "api_table_list",
          par: [
            { par: "db_name", va: database_name.value },
            { par: "search", va: options.value.SearchText },
            { par: "status", va: options.value.Status },
          ],
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];
        data.forEach((element) => {
          listTable.value.push({
            name: element.table_name,
            code: element.table_id,
          });
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
  })();
};
const listParameters = ref();
const CsharpType = ref([
  {
    name: "object",
  },
  {
    name: "string",
  },
  {
    name: "int",
  },
  {
    name: "datetime",
  },
  {
    name: "byte",
  },
  {
    name: "float",
  },

  {
    name: "double",
  },
  {
    name: "decimal",
  },
  {
    name: "bool",
  },
  {
    name: "char",
  },
]);
const listTable = ref([]);
const listService = ref([]);
const listCategorySave = ref([]);
const loadCategory = () => {
  let listCate = [];
  let listSer = [];
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
          proc: "api_service_list_all",
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

        listService.value = data;
        listSer = data;
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
    await axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "api_parameter_list_all",
          par: [
            { par: "search", va: options.value.SearchText },
            { par: "status", va: options.value.Status },
          ],
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];

        renderService(listCate, listSer, data);
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
const sttCate = ref(1);
const sttParam = ref(1);
const renderService = (listCate, listSer, listParam) => {
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
        if (listSer.length > 0) {
          let dsv = listSer.filter((x) => x.category_id == category_id);
          if (dsv.length > 0) {
            dsv.forEach((em) => {
              let om1 = { key: em.service_name, data: em };
              if (listParam.length > 0) {
                let dsp = listParam.filter(
                  (x) => x.service_id == em.service_id
                );
                if (dsp.length > 0) {
                  dsp.forEach((dspm) => {
                    let om2 = { key: dspm.parameters_name, data: dspm };
                    if (!om1.children) om1.children = [];
                    om1.children.push(om2);
                  });
                }
              }
              mm.children.push(om1);
            });
          }
        }
      };
      rechildren(om, m.category_id);
      arrChils.push(om);
    });
  //   arrtreeChils.unshift({ key: -1, data: -1, label: "-----Chọn Module----" });
  listCategory.value = arrChils;
};
const datalists = ref([]);
const dataListsParam = ref([]);
const checkNode = ref(false);
const onUnNodeSelect = () => {
  nodeSelected.value = null;
  isChirlden.value = false;
  checkNode.value = false;
  datalists.value = [];
  dataListsParam.value = [];
};
const isCheckParam = ref(false);
const nodeSelected = ref();
const nodeValue = ref();
const isTypeAPI = ref(true);
const selectedService_Id = ref();
const categoryName = ref();
const headerParam = ref("");
const categoryIdSave = ref();
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
  if (node.data.service_id) {
    headerParam.value = node.data.service_name;
    isTypeAPI.value = false;
    dataListsParam.value = [];
    selectedService_Id.value = node.data.service_id;
    isCheckParam.value = true;
    axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "api_parameter_list",
          par: [
            { par: "service_id", va: node.data.service_id },
            { par: "search", va: options.value.SearchText },
            { par: "status", va: options.value.Status },
          ],
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];
        if (data.length > 0) {
          data.forEach((element, i) => {
            element.is_order = i + 1;
            i++;
            dataListsParam.value.push(element);
          });
          sttParam.value = data.length + 1;
        } else {
          sttParam.value = 1;
          dataListsParam.value = [];
        }
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
  } else {
    if (node.data.category_id == categoryIdSave.value) {
      return;
    } else {
      isCheckParam.value = false;
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
              proc: "api_service_list",
              par: [
                { par: "category_id", va: node.data.category_id },
                { par: "search", va: options.value.SearchText },
                { par: "status", va: options.value.Status },
              ],
            },
            config
          )
          .then((response) => {
            let data = JSON.parse(response.data.data)[0];
            data.forEach((element) => {
              if (!element.parameters) element.parameters = [];
              listParameters.value.forEach((item) => {
                if (item.service_id == element.service_id) {
                  element.parameters.push(item);
                }
              });
              datalists.value.push(element);
            });

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
        options.value.totalRecords = datalists.value.length;
      })();
    }
  }
};
const expandedKeys = ref({});
const layout = ref("list");
const menuButMores = ref();
const checkCateEdit = ref();
const toggleMores = (event, u, check) => {
  if (check) {
    category.value = u;
    checkCateEdit.value = true;
  } else {
    service.value = u;
    checkCateEdit.value = false;
  }

  menuButMores.value.toggle(event);
};

const itemButMores = ref([
  {
    label: "Sửa",
    icon: "pi pi-cog",
    command: (event) => {
      if (checkCateEdit.value) {
        editCategory(category.value.category_id);
      } else {
        editService(service.value);
      }
    },
  },

  {
    label: "Xoá",
    icon: "pi pi-trash",
    command: (event) => {
      if (checkCateEdit.value) {
        deleteCategory(category.value.category_id);
      } else {
        deleteService(service.value.service_id);
      }
    },
  },
]);
const isChirlden = ref(false);
const nameParent = ref();

const rules = {
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
const ruleService = {
  service_name: {
    required,
    $errors: [
      {
        $property: "service_name",
        $validator: "required",
        $message: "Tên API không được để trống!",
      },
    ],
  },
};
const ruleParameter = {
  parameters_name: {
    required,
    $errors: [
      {
        $property: "parameters_name",
        $validator: "required",
        $message: "Tên tham số không được để trống!",
      },
    ],
  },
  parameters_type: {
    required,
    $errors: [
      {
        $property: "parameters_type",
        $validator: "required",
        $message: "Kiểu dữ liệu không được để trống!",
      },
    ],
  },
};

const category = ref({
  category_name: "",
  is_order: 1,
  status: true,
});
const service = ref({
  service_name: "",
  des: "",
  is_app: false,
  is_order: 1,
});
const parameter = ref({
  parameters_name: "",
  parameters_type: "",
  des: "",
  example_value: "",
  is_order: "",
  status: false,
  table_id: null,
});
const validateService = useVuelidate(ruleService, service);
const validateParameter = useVuelidate(ruleParameter, parameter);
const v$ = useVuelidate(rules, category);
const headerDialog = ref();
const displayBasic = ref();
const submitted = ref(false);
const openBasic = (str) => {
  submitted.value = false;
  headerDialog.value = str;
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
  displayBasic.value = true;
};
const closeDialog = () => {
  category.value = ref({
    category_name: "",
    is_order: 1,
    status: true,
  });
  displayBasic.value = false;
};
const editCategory = (value) => {
  submitted.value = false;
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "api_category_get",
        par: [{ par: "category_id", va: value }],
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data)[0];
      isChirlden.value = false;
      category.value = data[0];
      if (category.value.parent_id != null) {
        listCategorySave.value.forEach((element) => {
          if (element.category_id == category.value.parent_id) {
            nameParent.value = element.category_name;
            return;
          }
        });
        isChirlden.value = true;
      }
      headerDialog.value = "Sửa loại API";
      isSaveCategory.value = true;
      displayBasic.value = true;
    });
};
const deleteCategory = (value) => {
  swal
    .fire({
      title: "Thông báo",
      text: "Bạn có muốn xoá loại API này không!",
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
          .delete(baseURL + "/api/api_category/Delete_category", {
            headers: { Authorization: `Bearer ${store.getters.token}` },
            data: value != null ? [value] : 1,
          })
          .then((response) => {
            swal.close();
            if (response.data.err != "1") {
              swal.close();
              toast.success("Xoá loại API thành công!");
              nodeSelected.value = null;
              reloadService();
              loadProject();
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
const isSaveService = ref(false);
const isSaveCategory = ref(false);
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
          reloadService();
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
      .put(
        baseURL + "/api/api_category/Update_category",
        category.value,
        config
      )
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Sửa loại API thành công!");
          reloadService();
          loadProject();
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
const refreshTypeApi = () => {
  options.value.loading = true;
  loadProject();
  onNodeSelect(nodeValue.value);
};
const selectedKey = ref();
const headerAPI = ref();
const displayAPI = ref(false);
const addService = () => {
  submitted.value = false;
  headerAPI.value = "Thêm API";
  let sttSer;
  if (nodeSelected.value) {
    let stt = 0;
    listService.value.forEach((element) => {
      if (element.category_id == nodeSelected.value.category_id) {
        stt++;
      }
    });
    sttSer = stt + 1;
  }
  service.value = {
    service_name: "",
    des: "",
    is_app: false,
    is_order: sttSer,
    category_id: nodeSelected.value.category_id,
    status: true,
  };
  isSaveService.value = false;
  displayAPI.value = true;
};
const closeAPI = () => {
  service.value = {
    service_name: "",
    des: "",
    is_app: false,
    is_order: 1,
    category_id: null,
  };
  displayAPI.value = false;
};
const reloadParam = (service_id) => {
  options.value.loading = true;

  dataListsParam.value = [];
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "api_parameter_list",
        par: [
          { par: "service_id", va: service_id },
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
        i++;
        dataListsParam.value.push(element);
      });
      sttParam.value = data.length + 1;
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
};
const reloadService = () => {
  options.value.loading = true;
  datalists.value = [];
  (async () => {
    await axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "api_category_list",
          par: [
            { par: "parent_id", va: nodeSelected.value.category_id },
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
          proc: "api_service_list",
          par: [
            { par: "category_id", va: nodeSelected.value.category_id },
            { par: "search", va: options.value.SearchText },
            { par: "status", va: options.value.Status },
          ],
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];
        data.forEach((element) => {
          if (!element.parameters) element.parameters = [];
          listParameters.value.forEach((item) => {
            if (item.service_id == element.service_id) {
              element.parameters.push(item);
            }
          });
          datalists.value.push(element);
        });

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
    options.value.totalRecords = datalists.value.length;
  })();
};
const saveAPI = (isFormValid) => {
  service.value.des = service.value.des.replace("\n", "<br>");

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
  if (!isSaveService.value) {
    axios
      .post(baseURL + "/api/api_category/Add_service", service.value, config)
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Thêm API thành công!");
          loadProject();
          reloadService();
          closeAPI();
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
      .put(baseURL + "/api/api_category/Update_service", service.value, config)
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Cập nhật API thành công!");
          loadProject();
          reloadService();
          closeAPI();
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
const editService = (data) => {
  isSaveService.value = true;
  submitted.value = false;
  headerAPI.value = "Sửa API";
  data.des = data.des.replace("<br>", "\n");

  service.value = data;
  displayAPI.value = true;
};
const deleteService = (value) => {
  swal
    .fire({
      title: "Thông báo",
      text: "Bạn có muốn xoá API này không!",
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
          .delete(baseURL + "/api/api_category/Delete_service", {
            headers: { Authorization: `Bearer ${store.getters.token}` },
            data: value != null ? [value] : 1,
          })
          .then((response) => {
            swal.close();
            if (response.data.err != "1") {
              swal.close();
              toast.success("Xoá API thành công!");
              reloadService();
              loadProject();
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

const displayParameter = ref();
const headerParameter = ref(false);
const addParameter = () => {
  submitted.value = false;
  parameter.value = {
    service_id: selectedService_Id.value,
    parameters_name: "",
    parameters_type: "",
    des: "",
    example_value: "",
    is_order: sttParam.value,
    status: true,
    table_id: null,
  };

  headerParameter.value = "Thêm tham số";

  isSaveParameter.value = false;
  displayParameter.value = true;
};

const editParameter = (data) => {
  data.des = data.des.replace("<br>", "\n");
  parameter.value = data;
  headerParameter.value = "Sửa tham số";
  isSaveParameter.value = true;
  displayParameter.value = true;
};
const closeParameter = () => {
  parameter.value = {
    service_id: selectedService_Id.value,
    parameters_name: "",
    des: "",
    example_value: "",
    is_order: 1,
    status: false,
    table_id: null,
  };
  displayParameter.value = false;
};
const isSaveParameter = ref(false);
const saveParameter = (isFormValid) => {
  parameter.value.des = parameter.value.des.replace("\n", "<br>");
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
  if (!isSaveParameter.value) {
    axios
      .post(
        baseURL + "/api/api_category/Add_parameter",
        parameter.value,
        config
      )
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Thêm tham số thành công!");
          reloadParam(parameter.value.service_id);
          loadProject();
          closeParameter();
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
        baseURL + "/api/api_category/Update_parameter",
        parameter.value,
        config
      )
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Cập nhật tham số thành công!");
          reloadParam(parameter.value.service_id);
          loadProject();
          closeParameter();
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
const deleteParameter = (value) => {
  swal
    .fire({
      title: "Thông báo",
      text: "Bạn có muốn xoá tham số này không!",
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
          .delete(baseURL + "/api/api_category/Delete_parameter", {
            headers: { Authorization: `Bearer ${store.getters.token}` },
            data: value.parameters_id != null ? [value.parameters_id] : 1,
          })
          .then((response) => {
            swal.close();
            if (response.data.err != "1") {
              swal.close();
              toast.success("Xoá tham số thành công!");
              reloadParam(value.service_id);
              loadProject();
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
const showDetails = (value) => {
  datalists.value
    .filter((x) => x.active)
    .forEach(function (d) {
      d.active = false;
    });
  value.data.active = true;
  nodeValue.value = value;
  onNodeSelect(value);
};
const refreshService = () => {
  if (nodeValue.value) {
    onNodeSelect(nodeValue.value);
  }
};

//Xuất excelserviceButs
const serviceButs = ref();
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
  serviceButs.value.toggle(event);
};
const exportData = (method) => {
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  if (nodeValue.value != null) {
    axios
      .post(
        baseURL + "/api/Excel/ExportExcel",
        {
          excelname: "DANH SÁCH API",
          proc: "api_service_listexport",
          par: [{ par: "Category_id", va: nodeValue.value.data.category_id }],
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
  }
};
//

const paramButs = ref();
const itemParamButs = ref([
  {
    label: "Xuất Excel",
    icon: "pi pi-file-excel",
    command: (event) => {
      exportParamData("ExportExcel");
    },
  },
  {
    label: "Import Excel",
    icon: "pi pi-file-excel",
    command: (event) => {
      exportParamData("ExportExcel");
    },
  },
]);
const toggleParamExport = (event) => {
  paramButs.value.toggle(event);
};
const exportParamData = (method) => {
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  if (selectedService_Id.value != null) {
    axios
      .post(
        baseURL + "/api/Excel/ExportExcel",
        {
          excelname: "DANH SÁCH THAM SỐ",
          proc: "api_parameter_listexport",
          par: [{ par: "service_id", va: selectedService_Id.value }],
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
  }
};
const projectLogo = ref();
onMounted(() => {
  loadProject();
  return {
    loadProject,
    headerAPI,
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

          <div style="height: calc(100vh - 125px)">
            <TreeTable
              :value="listCategory"
              :filters="filters"
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
                          @click="openBasic('Thêm loại API')"
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
                    v-if="!data.node.data.service_id"
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
                    <div
                      v-if="!data.node.data.parameters_id"
                      class="flex w-full p-0"
                    >
                      <div class="grid w-full p-0">
                        <div
                          class="
                            field
                            col-12
                            md:col-12
                            w-full
                            flex
                            m-0
                            p-0
                            pt-2
                          "
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
                            <div
                              style="line-height: 30px; word-break: break-all"
                            >
                              {{ data.node.data.service_name }}
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>

                    <div v-else class="flex w-full p-0">
                      <div class="grid w-full p-0">
                        <div
                          class="
                            field
                            col-12
                            md:col-12
                            w-full
                            flex
                            m-0
                            p-0
                            pt-2
                          "
                        >
                          <div class="col-2 p-0">
                            <img
                              src="../../assets/image/paramester.png"
                              class="pr-2 pb-2"
                              width="28"
                              height="36"
                              style="object-fit: contain"
                            />
                          </div>
                          <div class="col-10 p-0">
                            <div
                              style="line-height: 30px; word-break: break-all"
                            >
                              {{ data.node.data.parameters_name }}
                            </div>
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
          <div>
            <div class="d-lang-table mx-3">
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
                      <i class="pi pi-sitemap py-3"></i> Danh sách API
                      <span v-if="options.totalRecords > 0"
                        >({{ options.totalRecords }})</span
                      >
                    </h3>
                    <Toolbar class="w-full custoolbar pt-5">
                      <template #start>
                        <span class="p-input-icon-left">
                          <i class="pi pi-search" />
                          <InputText
                            type="text"
                            class="p-inputtext-sm"
                            spellcheck="false"
                            placeholder="Tìm kiếm"
                          />
                        </span>
                      </template>

                      <template #end>
                        <DataViewLayoutOptions v-model="layout" class="mr-2" />

                        <Button
                          v-if="nodeSelected"
                          label="Thêm mới"
                          icon="pi pi-plus"
                          class="p-button-sm mr-2"
                          @click="addService"
                        />
                        <Button
                          class="
                            mr-2
                            p-button-sm p-button-outlined p-button-secondary
                          "
                          icon="pi pi-refresh"
                          @click="refreshService"
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
                          ref="serviceButs"
                          :popup="true"
                          :model="itemButs"
                        />
                      </template>
                    </Toolbar>
                  </div>
                </template>
                <template #grid="slotProps">
                  <div class="col-12 md:col-3 p-2">
                    <Card class="no-paddcontent">
                      <template #title>
                        <div style="position: relative">
                          <div
                            @click="showDetails(slotProps)"
                            class="cursor-pointer"
                          >
                            <div
                              class="
                                align-items-center
                                justify-content-center
                                text-center
                              "
                              v-if="slotProps.data.parameters_id"
                            >
                              <Avatar
                                image="./src/assets/image/paramester.png"
                                class="mr-2"
                                size="xlarge"
                                shape="circle"
                              />
                            </div>
                            <div v-else>
                              <div
                                class="
                                  align-items-center
                                  justify-content-center
                                  text-center
                                "
                                v-if="slotProps.data.project_id"
                              >
                                <Avatar
                                  image="./src/assets/image/folder.png"
                                  class="mr-2"
                                  size="xlarge"
                                  shape="square"
                                />
                              </div>
                              <div
                                v-if="!slotProps.data.project_id"
                                class="
                                  align-items-center
                                  justify-content-center
                                  text-center
                                "
                              >
                                <Avatar
                                  image="./src/assets/image/service.png"
                                  class="mr-2"
                                  size="xlarge"
                                  shape="circle"
                                />
                              </div>
                            </div>
                          </div>
                          <Button
                            style="position: absolute; right: 0px; top: 0px"
                            icon="pi pi-ellipsis-h"
                            class="p-button-rounded p-button-text ml-2"
                            @click="
                              toggleMores(
                                $event,
                                slotProps.data,
                                slotProps.data.project_id ? true : false
                              )
                            "
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
                        <div
                          @click="showDetails(slotProps)"
                          class="cursor-pointer"
                        >
                          <div v-if="!slotProps.data.project_id">
                            <i
                              v-if="slotProps.data.is_app"
                              class="pi pi-mobile"
                            ></i>
                            <i
                              v-else
                              class="pi pi-desktop"
                              style="color: transparent"
                            ></i>
                          </div>
                          <div v-else>
                            <i
                              class="pi pi-folder-open"
                              style="color: transparent"
                            ></i>
                          </div>
                        </div>
                      </template>
                      <template #content>
                        <div
                          class="text-center cursor-pointer"
                          @click="showDetails(slotProps)"
                        >
                          <div v-if="slotProps.data.parameters_id">
                            <div
                              class="text-lg text-blue-400 font-bold pb-2"
                              style="word-break: break-all"
                            >
                              {{ slotProps.data.parameters_name }}
                            </div>
                            <div v-html="slotProps.data.des"></div>
                          </div>
                          <div v-else>
                            <div
                              v-if="slotProps.data.project_id"
                              class="mb-1 text-lg text-blue-400 font-bold"
                              style="word-break: break-all"
                            >
                              {{ slotProps.data.category_name }}
                            </div>
                            <div
                              v-else
                              class="mb-1 text-lg text-blue-400 font-bold"
                              style="word-break: break-all"
                            >
                              {{ slotProps.data.service_name }}
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
                          pl-3
                          pt-3
                        "
                        :class="'row ' + slotProps.data.active"
                      >
                        <div class="col-12 field flex p-0 m-0 px-2">
                          <div
                            class="col-8 p-0 cursor-pointer"
                            @click="showDetails(slotProps)"
                          >
                            <div class="col-12 p-0 font-bold text-xl">
                              <div v-if="slotProps.data.parameters_id">
                                {{ slotProps.data.parameters_name }}
                              </div>
                              <div v-else>
                                <div
                                  v-if="slotProps.data.project_id"
                                  class="mb-1 font-bold text-xl"
                                >
                                  {{ slotProps.data.category_name }}
                                </div>
                                <div
                                  v-else
                                  class="mb-1 font-italic text-color-secondary"
                                >
                                  {{ slotProps.data.service_name }}
                                  <span v-if="slotProps.data.parameters.length > 0">
                                    (
                                    <small
                                      class="font-normal font-italic"
                                      v-for="(parameter, index) of slotProps
                                        .data.parameters"
                                    >
                                      <span class="text-primary">
                                        {{ parameter.parameters_type }}</span
                                      >
                                      {{ parameter.parameters_name
                                      }}<span
                                        v-if="
                                          index + 1 <
                                          slotProps.data.parameters.length
                                        "
                                        >,
                                      </span>
                                    </small>
                                    )
                                  </span>
                                </div>
                              </div>
                            </div>
                            <div class="col-12 p-0">
                              <div
                                v-if="slotProps.data.parameters_id"
                                v-html="slotProps.data.des"
                              ></div>
                              <div v-else>
                                <div
                                  v-if="slotProps.data.project_id"
                                  class="mb-1 font-italic text-color-secondary"
                                ></div>
                                <div
                                  v-else
                                  class="mb-1 font-italic text-color-secondary"
                                  v-html="slotProps.data.des"
                                ></div>
                              </div>
                            </div>
                          </div>
                          <div class="col-4 text-right flex">
                            <Toolbar
                              class="
                                w-full
                                surface-0
                                outline-none
                                border-none
                                p-0
                              "
                              :class="'row ' + slotProps.data.active"
                            >
                              <template #start> </template>
                              <template #end>
                                <div>
                                  <Button
                                    icon="pi pi-ellipsis-h"
                                    class="
                                      p-button-outlined p-button-secondary
                                      ml-2
                                      border-none
                                    "
                                    @click="
                                      toggleMores(
                                        $event,
                                        slotProps.data,
                                        slotProps.data.project_id ? true : false
                                      )
                                    "
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
                        <div
                          class="
                            col-12
                            field
                            flex
                            p-0
                            m-0
                            px-2
                            pb-2
                            cursor-pointer
                          "
                          @click="showDetails(slotProps)"
                        >
                          <div>
                            <div v-if="slotProps.data.parameters_id">
                              <img
                                src="../../assets/image/paramester.png"
                                width="28"
                                height="36"
                                style="object-fit: contain"
                              />
                            </div>
                            <div v-else>
                              <div v-if="slotProps.data.project_id">
                                <img
                                  src="../../assets/image/folder.png"
                                  width="28"
                                  height="36"
                                  style="object-fit: contain"
                                />
                              </div>
                              <div v-else>
                                <div v-if="slotProps.data.is_app">
                                  <Button class="p-0" type="button">APP</Button>
                                </div>
                                <div v-else>
                                  <Button
                                        
                                    class="border-none"
                                    type="button"
                                    style="color:transparent;background-color: transparent;"
                                    >APP</Button
                                  >
                                </div>
                              </div>
                            </div>
                          </div>
                          <div class="pl-8 pt-2">
                            <div v-if="slotProps.data.created_date">
                              <i
                                class="pi pi-calendar text-color-secondary"
                              ></i>
                              {{
                                moment(
                                  new Date(slotProps.data.created_date)
                                ).format("DD/MM/YYYY")
                              }}
                            </div>
                          </div>
                          <div class="px-8 pt-2">
                            <i class="pi pi-tags text-color-secondary"></i>
                            {{ categoryName }}
                          </div>
                          <div class="px-8 pt-2">
                            <div v-if="slotProps.data.created_by">
                              <i class="pi pi-user text-color-secondary"></i>
                              by: {{ slotProps.data.created_by }}
                            </div>
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
                    <img
                      src="../../assets/background/nodata.png"
                      height="144"
                    />
                    <h3 class="m-1">Không có dữ liệu</h3>
                  </div>
                </template>
              </DataView>
              <div>
                <Sidebar
                  v-model:visible="isCheckParam"
                  :baseZIndex="100"
                  position="right"
                  class="p-sidebar-lg"
                >
                  <div class="fixed top-0">
                    <h2>{{ headerParam }}</h2>
                  </div>

                  <div>
                    <DataTable
                      :value="dataListsParam"
                      :scrollable="true"
                      scrollHeight="flex"
                      :lazy="true"
                      :rowHover="true"
                      :showGridlines="true"
                      responsiveLayout="scroll"
                      :loading="options.loading"
                    >
                      <template #header>
                        <div v-if="isCheckParam">
                          <h3 class="m-0">
                            <i class="pi pi-share-alt py-3"></i> Danh sách tham
                            số
                          </h3>
                          <Toolbar class="w-full custoolbar pt-5">
                            <template #start>
                              <span class="p-input-icon-left">
                                <i class="pi pi-search" />
                                <InputText
                                  type="text"
                                  class="p-inputtext-sm"
                                  spellcheck="false"
                                  placeholder="Tìm kiếm"
                                />
                              </span>
                            </template>

                            <template #end>
                              <Button
                                label="Thêm mới"
                                icon="pi pi-plus"
                                class="mr-2 p-button-sm"
                                @click="addParameter"
                              />
                              <Button
                                class="
                                  mr-2
                                  p-button-sm
                                  p-button-outlined
                                  p-button-secondary
                                "
                                icon="pi pi-refresh"
                                @click="reloadParam(selectedService_Id)"
                              />

                              <Button
                                label="Tiện ích"
                                icon="pi pi-file-excel"
                                class="
                                  mr-2
                                  p-button-outlined p-button-secondary
                                "
                                aria-haspopup="true"
                                aria-controls="overlay_Export1"
                                @click="toggleParamExport"
                              />
                              <Menu
                                id="overlay_Export1"
                                ref="paramButs"
                                :popup="true"
                                :model="itemParamButs"
                              />
                            </template>
                          </Toolbar>
                        </div>
                      </template>

                      <Column
                        field="is_order"
                        header="STT"
                        headerStyle="text-align:center;max-width:75px;height:50px"
                        bodyStyle="text-align:center;max-width:75px"
                        class="
                          align-items-center
                          justify-content-center
                          text-center
                        "
                      >
                      </Column>
                      <Column
                        field="parameters_name"
                        header="Tên tham số"
                        headerStyle="height:50px"
                      >
                      </Column>
                      <Column
                        field="parameters_type"
                        header="Kiểu dữ liệu"
                        headerStyle="text-align:center;max-width:150px;height:50px"
                        bodyStyle="text-align:center;max-width:150px;"
                        class="
                          align-items-center
                          justify-content-center
                          text-center
                        "
                      >
                      </Column>

                      <Column
                        field="des"
                        header="Mô tả"
                        headerStyle="max-width:300px;height:50px"
                        bodyStyle="max-width:300px;"
                      >
                        <template #body="data">
                          <div v-html="data.data.des"></div>
                        </template>
                      </Column>
                      <Column
                        field="example_value"
                        header="Ví dụ"
                        class="
                          align-items-center
                          justify-content-center
                          text-center
                        "
                        headerStyle="text-align:center;max-width:150px;height:50px"
                        bodyStyle="text-align:center;max-width:150px;"
                      >
                      </Column>

                      <Column
                        header="Chức năng"
                        class="
                          align-items-center
                          justify-content-center
                          text-center
                        "
                        headerStyle="text-align:center;max-width:120px;height:50px"
                        bodyStyle="text-align:center;max-width:120px;"
                      >
                        <template #body="Dispatch">
                          <Button
                            @click="editParameter(Dispatch.data)"
                            class="
                              p-button-rounded
                              p-button-secondary
                              p-button-outlined
                              mx-1
                            "
                            type="button"
                            icon="pi pi-pencil"
                          ></Button>
                          <Button
                            @click="deleteParameter(Dispatch.data)"
                            class="
                              p-button-rounded
                              p-button-secondary
                              p-button-outlined
                              mx-1
                            "
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
                          <img
                            src="../../assets/background/nodata.png"
                            height="144"
                          />
                          <h3 class="m-1">Không có dữ liệu</h3>
                        </div>
                      </template>
                    </DataTable>
                  </div>
                </Sidebar>
              </div>
            </div>
          </div>
        </SplitterPanel>
      </Splitter>
    </div>
  </div>
  <Dialog
    :header="headerDialog"
    v-model:visible="displayBasic"
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
            :class="{ 'p-invalid': v$.category_name.$invalid && submitted }"
          />
        </div>
        <div style="display: flex" class="field col-12 md:col-12">
          <div class="col-3 text-left"></div>
          <small
            v-if="
              (v$.category_name.$invalid && submitted) ||
              v$.category_name.$pending.$response
            "
            class="col-8 p-error p-0"
          >
            <span class="col-12 p-0">{{
              v$.category_name.required.$message
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
        @click="closeDialog"
        class="p-button-text"
      />

      <Button
        label="Lưu"
        icon="pi pi-check"
        @click="saveCategory(!v$.$invalid)"
      />
    </template>
  </Dialog>
  <Dialog
    :header="headerAPI"
    v-model:visible="displayAPI"
    :style="{ width: '40vw' }"
  >
    <form>
      <div class="grid formgrid m-2">
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left p-0"
            >Tên API <span class="redsao">(*)</span></label
          >
          <InputText
            v-model="service.service_name"
            spellcheck="false"
            class="col-10 ip36 px-2"
            :class="{
              'p-invalid': validateService.service_name.$invalid && submitted,
            }"
          />
        </div>
        <div style="display: flex" class="field col-12 md:col-12">
          <div class="col-2 text-left"></div>
          <small
            v-if="
              (validateService.service_name.$invalid && submitted) ||
              validateService.service_name.$pending.$response
            "
            class="col-8 p-error p-0"
          >
            <span class="col-12 p-0">{{
              validateService.service_name.required.$message
                .replace("Value", "Tên API")
                .replace("is required", "không được để trống")
            }}</span>
          </small>
        </div>
         <div class="field col-12 md:col-12">
          <label class="col-2 text-left p-0"
            >Tên thủ tục </label
          >
          <InputText
            v-model="service.proc_name"
            spellcheck="false"
            class="col-10 ip36 px-2"
          
          />
        </div>
        <div style="display: flex" class="col-12 field md:col-12">
          <div class="field col-4 md:col-4 p-0">
            <label class="col-6 text-left p-0">Số thứ tự </label>
            <InputNumber v-model="service.is_order" class="col-6 ip36 p-0" />
          </div>
          <div class="field col-4 md:col-4 p-0">
            <label
              style="vertical-align: text-bottom"
              class="col-6 text-center p-0"
              >App</label
            >
            <InputSwitch v-model="service.is_app" class="col-6" />
          </div>
          <div class="field col-4 md:col-4 p-0">
            <label
              style="vertical-align: text-bottom"
              class="col-6 text-center p-0"
              >Trạng thái
            </label>
            <InputSwitch v-model="service.status" class="col-6" />
          </div>
        </div>
        <div class="field col-12 md:col-12 flex">
          <label class="col-2 text-left p-0">Mô tả</label>
          <div class="col-10 p-0">
            <!-- <Editor v-model="service.des" editorStyle="height: 150px"/> -->
            <Textarea
              spellcheck="false"
              v-model="service.des"
              class="col-12 ip36 p-2"
              autoResize
            />
          </div>
        </div>
           <div class="field col-12 md:col-12 flex">
          <label class="col-2 text-left p-0">Dữ liệu trả về</label>
          <div class="col-10 p-0">
            <Editor v-model="service.data" editorStyle="height: 150px"/>
           
          </div>
        </div>
      </div>
    </form>
    <template #footer>
      <Button
        label="Hủy"
        icon="pi pi-times"
        @click="closeAPI()"
        class="p-button-text"
      />

      <Button
        label="Lưu"
        icon="pi pi-check"
        @click="saveAPI(!validateService.$invalid)"
      />
    </template>
  </Dialog>
  <Dialog
    :header="headerParameter"
    v-model:visible="displayParameter"
    :style="{ width: '40vw' }"
  >
    <form>
      <div class="grid formgrid m-2">
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left p-0"
            >Tên tham số <span class="redsao">(*)</span></label
          >
          <InputText
            v-model="parameter.parameters_name"
            spellcheck="false"
            class="col-10 ip36 px-2"
            :class="{
              'p-invalid':
                validateParameter.parameters_name.$invalid && submitted,
            }"
          />
        </div>

        <div style="display: flex" class="field col-12 md:col-12">
          <div class="col-2 text-left"></div>
          <small
            v-if="
              (validateParameter.parameters_name.$invalid && submitted) ||
              validateParameter.parameters_name.$pending.$response
            "
            class="col-8 p-error p-0"
          >
            <span class="col-12 p-0">{{
              validateParameter.parameters_name.required.$message
                .replace("Value", "Tên tham số")
                .replace("is required", "không được để trống")
            }}</span>
          </small>
        </div>
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left p-0"
            >Kiểu dữ liệu <span class="redsao">(*)</span></label
          >
          <Dropdown
            v-model="parameter.parameters_type"
            :options="CsharpType"
            optionLabel="name"
            optionValue="name"
            placeholder="Chọn kiểu dữ liệu hoặc nhập"
            :editable="true"
            :filter="true"
            spellcheck="false"
            class="col-10 ip36 p-0"
          >
          </Dropdown>
        </div>

        <div style="display: flex" class="field col-12 md:col-12">
          <div class="col-2 text-left"></div>
          <small
            v-if="
              (validateParameter.parameters_type.$invalid && submitted) ||
              validateParameter.parameters_type.$pending.$response
            "
            class="col-8 p-error p-0"
          >
            <span class="col-12 p-0">{{
              validateParameter.parameters_type.required.$message
                .replace("Value", "Kiểu dữ liệu ")
                .replace("is required", "không được để trống")
            }}</span>
          </small>
        </div>
        <div style="display: flex" class="col-12 field md:col-12">
          <label class="col-2 text-left p-0">Bảng</label>
          <Dropdown
            v-model="parameter.table_id"
            :options="listTable"
            optionLabel="name"
            optionValue="code"
            placeholder="Chọn bảng(nếu có)"
            
            :filter="true"
            spellcheck="false"
            class="col-10 ip36 p-0"
          >
          </Dropdown>
        </div>
        <div class="field col-12 md:col-12 flex">
          <label class="col-2 text-left p-0">Mô tả</label>
          <div class="col-10 p-0">
            <!-- <Editor v-model="service.des" editorStyle="height: 150px"/> -->
            <Textarea
              v-model="parameter.des"
              class="col-12 ip36 p-2"
              autoResize
              spellcheck="false"
            />
          </div>
        </div>
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left p-0">Ví dụ</label>
          <InputText
            v-model="parameter.example_value"
            spellcheck="false"
            class="col-10 ip36 px-2"
          />
        </div>

        <div style="display: flex" class="col-12 field md:col-12">
          <div class="field col-6 md:col-6 p-0">
            <label class="col-4 text-left p-0">Số thứ tự </label>
            <InputNumber v-model="parameter.is_order" class="col-8 ip36 p-0" />
          </div>

          <div class="field col-6 md:col-6 p-0">
            <label
              style="vertical-align: text-bottom"
              class="col-6 text-center p-0"
              >Trạng thái
            </label>
            <InputSwitch v-model="parameter.status" class="col-6" />
          </div>
        </div>
      </div>
    </form>
    <template #footer>
      <Button
        label="Hủy"
        icon="pi pi-times"
        @click="closeParameter()"
        class="p-button-text"
      />

      <Button
        label="Lưu"
        icon="pi pi-check"
        @click="saveParameter(!validateParameter.$invalid)"
      />
      <!-- -->
    </template>
  </Dialog>
</template>

<style scoped>
.d-lang-table {
  height: calc(100vh - 52px);
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

.row.true {
  background-color: rgb(190, 211, 245) !important;
}
</style>