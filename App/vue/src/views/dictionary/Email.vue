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
  email_group_name: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.STARTS_WITH }],
  },
  email_name: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.STARTS_WITH }],
  },
  full_name: {
    operator: FilterOperator.AND,
    constraints: [{ value: null, matchMode: FilterMatchMode.STARTS_WITH }],
  },
});
const rules = {
  email_group_name: {
    required,
    $errors: [
      {
        $property: "email_group_name",
        $validator: "required",
        $message: "Tên nhóm email không được để trống!",
      },
    ],
  },
};

const rulesEmail = {
  email_name: {
    required,
    $errors: [
      {
        $property: "email_name",
        $validator: "required",
        $message: "Tên email không được để trống!",
      },
    ],
  },
  full_name: {
    required,
    $errors: [
      {
        $property: "full_name",
        $validator: "required",
        $message: "Họ tên email không được để trống!",
      },
    ],
  },
};
const EmailGroup = ref({
  email_group_name: "",
  status: true,
  is_order: 1,
});
const Email = ref({
  email_name: "",
  full_name: "",
  description: "",
  status: true,
  is_order: 1,
});
const selectedEmailGroups = ref();
const selectedEmail = ref();
const submitted = ref(false);
const v$ = useVuelidate(rules, EmailGroup);
const validateEmail = useVuelidate(rulesEmail, Email);
const issaveEmailGroup = ref(false);
const issaveEmail = ref(false);
const datalists = ref();
const toast = useToast();
const basedomainURL = baseURL;
const checkDelList = ref(false);
const checkDelListEmail = ref(false);
const options = ref({
  IsNext: true,
  sort: "is_order",
  SearchText: "",
  PageNo: 0,
  PageSize: 20,
  loading: true,
  totalRecords: null,
});
const optionsEmail = ref({
  IsNext: true,
  sort: "is_order",
  SearchText: "",
  PageNo: 0,
  PageSize: 20,
  loading: true,
  totalRecords: 0,
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
        proc: "ca_email_group_count",
        par: [{ par: "search", va: options.value.SearchText }],
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data)[0];
      if (data.length > 0) {
        options.value.totalRecords = data[0].totalRecords;
        sttEmailGroup.value = options.value.totalRecords + 1;
      }
    })
    .catch((error) => {
      addLog({
        title: "Lỗi Console loadCount",
        controller: "Email.vue",
        logcontent: error.message,
        loai: 2,
      });
    });
};
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
          proc: "ca_email_group_list",
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
        console.log("error", error);
        addLog({
          title: "Lỗi Console loadData",
          controller: "Email.vue",
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
     options.value.id = null;
    options.value.IsNext = true;
  options.value.PageNo = event.page+1;
   
  loadDataSQL();
};
const onPageEmail = (event) => {
    options.value.id = null;
    options.value.IsNext = true;
  options.value.PageNo = event.page+1;
  
  loadEmailSQL();
};
//Hiển thị dialog
const headerDialog = ref();
const headerAddEmail = ref();
const displayBasic = ref(false);
const displayEmail = ref(false);
const addEmail = (str) => {
  submitted.value = false;
  Email.value = {
    email_name: "",
    full_name: "",
    description: "",
    status: true,
    is_order: sttEmail.value,
    email_group_id: emailGroupID.value,
  };
  if (store.state.user.IsAdmin) {
    Email.value.unit_id = 0;
  }
  issaveEmail.value = false;
  headerAddEmail.value = str;
  displayEmail.value = true;
};
const openBasic = (str) => {
  submitted.value = false;
  EmailGroup.value = {
    email_group_name: "",
    is_order: sttEmailGroup.value,
    status: true,
  };
  if (store.state.user.IsAdmin) {
    EmailGroup.value.unit_id = 0;
  }
  issaveEmailGroup.value = false;
  headerDialog.value = str;
  displayBasic.value = true;
};
const closeDialog = () => {
  EmailGroup.value = {
    email_group_name: "",
    is_order: 1,
    status: true,
  };
  displayBasic.value = false;
};
const closeDialogEmail = () => {
  Email.value = {
    email_name: "",
    full_name: "",
    description: "",
    status: true,
    is_order: 1,
  };
  displayEmail.value = false;
};
//Thêm bản ghi
const saveEmailGroup = (isFormValid) => {
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
  if (!issaveEmailGroup.value) {
    axios
      .post(
        baseURL + "/api/ca_email_groups/Add_email_groups",
        EmailGroup.value,
        config
      )
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Thêm nhóm Email thành công!");
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
      .put(
        baseURL + "/api/ca_email_groups/Update_email_groups",
        EmailGroup.value,
        config
      )
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Sửa nhóm email thành công!");
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

const saveEmail = (isFormValid) => {
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
  if (!issaveEmail.value) {
    axios
      .post(baseURL + "/api/ca_emails/Add_email", Email.value, config)
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Thêm Email thành công!");
          refreshEmail(emailGroupID.value);
          loadData(true);
          closeDialogEmail();
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
      .put(baseURL + "/api/ca_emails/Update_emails", Email.value, config)
      .then((response) => {
        if (response.data.err != "1") {
          swal.close();
          toast.success("Sửa email thành công!");
          refreshEmail(emailGroupID.value);
          closeDialogEmail();
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
const sttEmailGroup = ref();
const sttEmail = ref();
//Sửa bản ghi
const editEmailGroup = (dataEmailGroup) => {
  submitted.value = false;
  EmailGroup.value = dataEmailGroup;
  headerDialog.value = "Sửa nhóm email";
  issaveEmailGroup.value = true;
  displayBasic.value = true;
};
const editEmail = (dataEmail) => {
  submitted.value = false;
  Email.value = dataEmail;
  headerAddEmail.value = "Sửa Email";
  issaveEmail.value = true;
  displayEmail.value = true;
};
//Xóa bản ghi
const delEmailGroup = (EmailGroup) => {
  swal
    .fire({
      title: "Thông báo",
      text: "Bạn có muốn xoá nhóm email này không!",
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
          .delete(baseURL + "/api/ca_email_groups/Delete_email_groups", {
            headers: { Authorization: `Bearer ${store.getters.token}` },
            data: EmailGroup != null ? [EmailGroup.email_group_id] : 1,
          })
          .then((response) => {
            swal.close();
            if (response.data.err != "1") {
              swal.close();
              toast.success("Xoá nhóm email thành công!");
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

const delEmail = (Email) => {
  swal
    .fire({
      title: "Thông báo",
      text: "Bạn có muốn xoá email này không!",
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
          .delete(baseURL + "/api/ca_emails/Delete_emails", {
            headers: { Authorization: `Bearer ${store.getters.token}` },
            data: Email != null ? [Email.email_id] : 1,
          })
          .then((response) => {
            swal.close();
            if (response.data.err != "1") {
              swal.close();
              toast.success("Xoá email thành công!");
              refreshEmail(emailGroupID.value);
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
      exportData("ExportExcel");
    },
  },
]);
const itemButEmails = ref([
  {
    label: "Xuất Excel",
    icon: "pi pi-file-excel",
    command: (event) => {
      exportDataEmail("ImportExcel");
    },
  },
]);
const toggleExport = (event) => {
  menuButs.value.toggle(event);
};

const exportDataEmail = (method) => {
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
        excelname: "DANH SÁCH EMAIL",
        proc: "ca_email_listexport",
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
        excelname: "DANH SÁCH NHÓM EMAIL",
        proc: "ca_email_groups_listexport",
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
  if (options.value.PageNo == 0) {
    options.value.PageNo += 1;
  }
  console.log("logg", options.value.PageNo);
  options.value.sort =
    event.sortField + (event.sortOrder == 1 ? " ASC" : " DESC");
  if (event.sortField != "email_group_id") {
    options.value.sort +=
      ",email_group_id " + (event.sortOrder == 1 ? " ASC" : " DESC");
  }
  isDynamicSQL.value = true;
  loadDataSQL();
};

const onSortEmail = (event) => {
  if (optionsEmail.value.PageNo == 0) {
    optionsEmail.value.PageNo += 1;
  }

  optionsEmail.value.sort =
    event.sortField + (event.sortOrder == 1 ? " ASC" : " DESC");
  if (event.sortField != "email_id") {
    optionsEmail.value.sort +=
      ",email_id " + (event.sortOrder == 1 ? " ASC" : " DESC");
  }
  loadEmailSQL();
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
    .post(baseURL + "/api/SQL/Filter_Email_Groups", data, config)
    .then((response) => {
      let dt = JSON.parse(response.data.data);
      let data = dt[0];
      if (data.length > 0) {
        if (options.value.PageNo == 0) {
          options.value.PageNo = 1;
        }
        data.forEach((element, i) => {
          element.is_order =
            options.value.PageNo * options.value.PageSize -
            options.value.PageSize +
            i +
            1;
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

const loadEmailSQL = () => {
  let data = {
    id: optionsEmail.value.id,
    next: optionsEmail.value.IsNext,
    sqlO: optionsEmail.value.sort,
    Search: optionsEmail.value.SearchText,
    PageNo: optionsEmail.value.PageNo,
    PageSize: optionsEmail.value.PageSize,
    fieldSQLS: filterSQL.value,
  };

  optionsEmail.value.loading = true;
  axios
    .post(baseURL + "/api/SQL/Filter_Email", data, config)
    .then((response) => {
      let dt = JSON.parse(response.data.data);
      let data = dt[0];
      if (data.length > 0) {
        if (optionsEmail.value.PageNo == 0) {
          optionsEmail.value.PageNo = 1;
        }
        data.forEach((element, i) => {
          element.is_order =
            optionsEmail.value.PageNo * optionsEmail.value.PageSize -
            optionsEmail.value.PageSize +
            i +
            1;
        });

        emailList.value = data;
      } else {
        emailList.value = [];
      }
      optionsEmail.value.loading = false;
      //Show Count nếu có
      if (dt.length == 2) {
        optionsEmail.value.totalRecords = dt[1][0].totalRecords;
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
const searchEmailGroups = (event) => {
  if (event.code == "Enter") {
    isDynamicSQL.value = true;
    loadData(true);
  }
};
const searchEmail = (event) => {
  if (event.code == "Enter") {
    refreshEmail(emailGroupID.value);
  }
};
const onFilter = (event) => {
  filterSQL.value = [];
  for (const [key, value] of Object.entries(event.filters)) {
    if (key != "global") {
      let obj = {
        key: key != "email_group_name" ? "email_group_name" : key,
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
  options.value.PageNo = 0;
  options.value.id = null;
  isDynamicSQL.value = true;
  loadDataSQL();
};

const onFilterEmail = (event) => {
  filterSQL.value = [];
  for (const [key, value] of Object.entries(event.filters)) {
    if (key != "global") {
      let obj = {
        key: key,
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
  optionsEmail.value.PageNo = 0;
  optionsEmail.value.id = null;
  loadEmailSQL();
};
//Checkbox
const onCheckBox = (value) => {
  let data = {
    IntID: value.email_group_id,
    TextID: value.email_group_id + "",
    IntTrangthai: 1,
    BitTrangthai: value.status,
  };
  axios
    .put(
      baseURL + "/api/ca_email_groups/Update_StatusGroup_Email",
      data,
      config
    )
    .then((response) => {
      if (response.data.err != "1") {
        swal.close();
        toast.success("Sửa nhóm email thành công!");
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

const onCheckBoxEmail = (value) => {
  let data = {
    IntID: value.email_group_id,
    TextID: value.email_group_id + "",
    IntTrangthai: 1,
    BitTrangthai: value.status,
  };
  axios
    .put(baseURL + "/api/ca_emails/Update_StatusEmail", data, config)
    .then((response) => {
      if (response.data.err != "1") {
        swal.close();
        toast.success("Sửa email thành công!");
        refreshEmail(emailGroupID.value);
        closeDialogEmail();
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
const deleteEmailList = () => {
  let listId = new Array(selectedEmail.length);
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
        selectedEmail.value.forEach((item) => {
          listId.push(item.email_id);
        });
        axios
          .delete(baseURL + "/api/ca_emails/Delete_emails", {
            headers: { Authorization: `Bearer ${store.getters.token}` },
            data: listId != null ? listId : 1,
          })
          .then((response) => {
            swal.close();
            if (response.data.err != "1") {
              swal.close();
              toast.success("Xoá danh sách thành công!");
              checkDelListEmail.value = false;

              loadData(true);
              refreshEmail(emailGroupID.value);
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

const deleteList = () => {
  let listId = new Array(selectedEmailGroups.length);
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
        selectedEmailGroups.value.forEach((item) => {
          listId.push(item.email_group_id);
        });
        axios
          .delete(baseURL + "/api/ca_email_groups/Delete_email_groups", {
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
const reFilterNews = () => {
  filterPhanloai.value = null;
  filterTrangthai.value = null;
  filterGroupEmails();
  showFilter.value = false;
};
const reFilterEmail = () => {
  filterPhanloai.value = null;
  filterTrangthai.value = null;
  filterEmails();
  showFilter.value = false;
};
const filterGroupEmails = () => {
  filterSQL.value = [];
  let arr = [];
  let obj = {};
  let obj1 = {};
  if (filterPhanloai.value != null) {
    obj.key = "unit_id";
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
  isDynamicSQL.value = true;
  loadDataSQL();
  showFilter.value = false;
};

const filterEmails = () => {
  filterSQL.value = [];
  let arr = [];
  let obj = {};
  let obj1 = {};
  if (filterPhanloai.value != null) {
    obj.key = "unit_id";
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
  loadEmailSQL();
  showFilter.value = false;
};
const isShowEmail = ref(false);
const emailList = ref();
const emailGroupID = ref();
const refreshEmail = (value) => {
  emailGroupID.value = value;
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "ca_email_list",
        par: [
          { par: "group_id", va: value },
          { par: "pageno", va: optionsEmail.value.PageNo },
          { par: "pagesize", va: optionsEmail.value.PageSize },
          { par: "search", va: optionsEmail.value.SearchText },
          { par: "status", va: optionsEmail.value.Status },
        ],
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data)[0];
      emailList.value = data;
      isShowEmail.value = true;
      optionsEmail.value.loading = false;
    })
    .catch((error) => {
      toast.error("Tải dữ liệu không thành công!");
      optionsEmail.value.loading = false;
      console.log("error", error);
      addLog({
        title: "Lỗi Console loadData",
        controller: "Email.vue",
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

const loadEmailCount = () => {
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "ca_email_count",
        par: [{ par: "email_group_id", va: emailGroupID.value }],
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data)[0];
      optionsEmail.totalRecords = data[0].totalRecords;
    })
    .catch((error) => {
      toast.error("Tải dữ liệu không thành công!");
      optionsEmail.value.loading = false;
      console.log("error", error);
      addLog({
        title: "Lỗi Console loadData",
        controller: "Email.vue",
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
const showEmails = (value) => {

  if (emailGroupID.value == value) {
    if (isShowEmail.value == false) {
      isShowEmail.value = true;
    } else {
      isShowEmail.value = false;
    }
    return;
  } else {
    document.getElementById(value).style.backgroundColor='red';
    emailGroupID.value = value;
    if (isShowEmail.value == true) options.value.loading = false;
    else options.value.loading = true;
    optionsEmail.value.loading = true;
    axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "ca_email_count",
          par: [
            { par: "email_group_id", va: emailGroupID.value },
            { par: "search", va: options.value.SearchText },
          ],
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];
        optionsEmail.value.totalRecords = data[0].totalRecords;
        sttEmail.value = data[0].totalRecords + 1;
      });
    axios
      .post(
        baseURL + "/api/Proc/CallProc",
        {
          proc: "ca_email_list",
          par: [
            { par: "group_id", va: value },
            { par: "pageno", va: optionsEmail.value.PageNo },
            { par: "pagesize", va: optionsEmail.value.PageSize },
            { par: "search", va: optionsEmail.value.SearchText },
            { par: "status", va: optionsEmail.value.Status },
          ],
        },
        config
      )
      .then((response) => {
        let data = JSON.parse(response.data.data)[0];
        emailList.value = data;

        isShowEmail.value = true;
        optionsEmail.value.loading = false;
        options.value.loading = false;
      })
      .catch((error) => {
        toast.error("Tải dữ liệu không thành công!");
        optionsEmail.value.loading = false;
        console.log("error", error);
        addLog({
          title: "Lỗi Console loadData",
          controller: "Email.vue",
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
watch(selectedEmailGroups, () => {
  if (selectedEmailGroups.value.length > 0) {
    checkDelList.value = true;
  } else {
    checkDelList.value = false;
  }
});
watch(selectedEmail, () => {
  if (selectedEmail.value.length > 0) {
    checkDelListEmail.value = true;
  } else {
    checkDelListEmail.value = false;
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
    saveEmailGroup,
    isFirst,
    searchEmailGroups,
    onCheckBox,
    selectedEmailGroups,
    deleteList,
  };
});
</script>
<template>
  <div class="surface-100">
    <div class="h-2rem p-3 pb-0 m-3 mb-0 surface-0">
      <h3 class="m-0" v-if="!isShowEmail">
        <i class="pi pi-at"></i> Danh sách nhóm Email ({{
          options.totalRecords
        }})
      </h3>
      <h3 class="m-0" v-else><i class="pi pi-at"></i> Danh sách Email</h3>
      <!-- ({{
          optionsEmail.totalRecords
        }}) -->
    </div>
    <Toolbar
      class="outline-none mx-3 surface-0 border-none"
      v-if="!isShowEmail"
    >
      <template #start>
        <span class="p-input-icon-left">
          <i class="pi pi-search" />
          <InputText
            v-model="options.SearchText"
            @keypress="searchEmailGroups"
            type="text"
            spellcheck="false"
            placeholder="Tìm kiếm nhóm email"
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
                          @click="reFilterNews"
                          class="p-button-outlined"
                          label="Xóa"
                        ></Button>
                      </template>
                      <template #end>
                        <Button @click="filterGroupEmails" label="Lọc"></Button>
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
          @click="openBasic('Thêm mới nhóm Email')"
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
    <Toolbar class="outline-none mx-3 surface-0 border-none" v-else>
      <template #start>
        <span class="p-input-icon-left">
          <i class="pi pi-search" />
          <InputText
            v-model="optionsEmail.SearchText"
            @keypress="searchEmail"
            type="text"
            spellcheck="false"
            placeholder="Tìm kiếm Email"
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
                        <Button @click="filterEmails" label="Lọc"></Button>
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
          v-if="checkDelListEmail"
          @click="deleteEmailList()"
          label="Xóa"
          icon="pi pi-trash"
          class="mr-2 p-button-danger"
        />
        <Button
          @click="addEmail('Thêm mới Email')"
          label="Thêm mới"
          icon="pi pi-plus"
          class="mr-2"
        />
        <Button
          @click="refreshEmail(emailGroupID)"
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
    <div class="d-lang-table mx-3 flex">
      <DataTable
        :lazy="true"
        @page="onPage($event)"
        @filter="onFilter($event)"
        @sort="onSort($event)"
        :value="datalists"
        :loading="options.loading"
        :paginator="options.totalRecords > options.PageSize"
        :rows="options.PageSize"
        :totalRecords="options.totalRecords"
        dataKey="email_group_id"
        :rowHover="true"
        v-model:filters="filters"
        filterDisplay="menu"
        :showGridlines="true"
        filterMode="lenient"
        paginatorTemplate="FirstPageLink PrevPageLink PageLinks  NextPageLink LastPageLink"
        responsiveLayout="scroll"
        :scrollable="true"
        scrollHeight="flex"
        :globalFilterFields="['email_group_name']"
        v-model:selection="selectedEmailGroups"
        :class="isShowEmail == true ? 'w-5 pt-2' : 'w-full'"
      >
        <Column
          v-if="!isShowEmail"
          selectionMode="multiple"
          headerStyle="text-align:center;max-width:75px;height:50px"
          bodyStyle="text-align:center;max-width:75px;;max-height:60px"
        ></Column>
        <Column
          field="is_order"
          header="STT"
          :sortable="true"
          headerStyle="text-align:center;max-width:75px;height:50px"
          bodyStyle="text-align:center;max-width:75px;;max-height:60px"
          class="align-items-center justify-content-center text-center"
        >
        </Column>

        <Column
          field="email_group_name"
          header="Nhóm Email"
          :sortable="true"
          headerStyle="height:50px"
          bodyStyle="max-height:60px"
        >
          <template #body="{ data }">
            {{ data.email_group_name }}
          </template>
          <template #filter="{ filterModel }">
            <InputText
              type="text"
              v-model="filterModel.value"
              class="p-column-filter"
              placeholder="Từ khoá"
            /> </template
        ></Column>
        <Column
          field="email_count"
          header="Số email"
          headerStyle="text-align:center;max-width:120px;height:50px"
          bodyStyle="text-align:center;max-width:120px;;max-height:60px"
          class="align-items-center justify-content-center text-center"
        >
          <template #body="data">
            <div>
              <Button
               :id="data.data.email_group_id"
                class="p-button-rounded p-button-success"
                @click="showEmails(data.data.email_group_id)"
              >
                {{ data.data.email_count }}</Button
              >
            </div>
          </template>
        </Column>
        <Column
          v-if="!isShowEmail"
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
          v-if="!isShowEmail"
          field="unit_id"
          header="Hệ thống"
          headerStyle="text-align:center;max-width:125px;height:50px"
          bodyStyle="text-align:center;max-width:125px;;max-height:60px"
          class="align-items-center justify-content-center text-center"
        >
          <template #body="data">
            <div v-if="data.data.unit_id == 0">
              <i
                class="pi pi-check text-blue-400"
                style="font-size: 1.5rem"
              ></i>
            </div>
            <div v-else></div>
          </template>
        </Column>
        <Column
          v-if="!isShowEmail"
          header="Chức năng"
          class="align-items-center justify-content-center text-center"
          headerStyle="text-align:center;max-width:120px;height:50px;min-width:150px;"
          bodyStyle="text-align:center;max-width:120px;;max-height:60px;min-width:150px"
        >
          <template #body="EmailGroup">
            <div
              v-if="EmailGroup.data.unit_id != null || store.state.user.IsAdmin"
            >
              <Button
                @click="editEmailGroup(EmailGroup.data)"
                class="
                  p-button-rounded p-button-secondary p-button-outlined
                  mx-1
                "
                type="button"
                icon="pi pi-pencil"
              ></Button>
              <Button
                @click="delEmailGroup(EmailGroup.data, true)"
                class="
                  p-button-rounded p-button-secondary p-button-outlined
                  mx-1
                "
                type="button"
                icon="pi pi-trash"
              ></Button>
            </div>
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

      <div class="w-full pt-2" v-if="isShowEmail">
        <DataTable
          :lazy="true"
          @page="onPageEmail($event)"
          @filter="onFilterEmail($event)"
          @sort="onSortEmail($event)"
          :value="emailList"
          :loading="optionsEmail.loading"
          :paginator="optionsEmail.totalRecords > optionsEmail.PageSize"
          :rows="optionsEmail.PageSize"
          :totalRecords="optionsEmail.totalRecords"
          dataKey="email_id"
          :rowHover="true"
          v-model:filters="filters"
          filterDisplay="menu"
          :showGridlines="true"
          filterMode="lenient"
          paginatorTemplate="FirstPageLink PrevPageLink PageLinks  NextPageLink LastPageLink"
          responsiveLayout="scroll"
          :scrollable="true"
          scrollHeight="flex"
          :globalFilterFields="['email_name', 'full_name']"
          v-model:selection="selectedEmail"
          class="w-full"
        >
          <Column
            selectionMode="multiple"
            headerStyle="text-align:center;max-width:50px;height:50px"
            bodyStyle="text-align:center;max-width:50px;;max-height:60px"
          ></Column>
          <Column
            field="is_order"
            header="STT"
            :sortable="true"
            headerStyle="text-align:center;max-width:75px;height:50px"
            bodyStyle="text-align:center;max-width:75px;;max-height:60px"
            class="align-items-center justify-content-center text-center"
          >
          </Column>

          <Column
            field="email_name"
            header="Email"
            :sortable="true"
            headerStyle="height:50px"
            bodyStyle="max-height:60px"
          >
            <template #body="{ data }">
              {{ data.email_name }}
            </template>
            <template #filter="{ filterModel }">
              <InputText
                type="text"
                v-model="filterModel.value"
                class="p-column-filter"
                placeholder="Từ khoá"
              /> </template
          ></Column>

          <Column
            field="full_name"
            header="Họ và tên"
            :sortable="true"
            headerStyle="height:50px"
            bodyStyle="max-height:60px"
          >
            <template #body="{ data }">
              {{ data.full_name }}
            </template>
            <template #filter="{ filterModel }">
              <InputText
                type="text"
                v-model="filterModel.value"
                class="p-column-filter"
                placeholder="Từ khoá"
              /> </template
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
                :binary="data.data.status"
                v-model="data.data.status"
                @click="onCheckBoxEmail(data.data)"
              />
            </template>
          </Column>
          <Column
            field="unit_id"
            header="Hệ thống"
            headerStyle="text-align:center;max-width:125px;height:50px"
            bodyStyle="text-align:center;max-width:125px;;max-height:60px"
            class="align-items-center justify-content-center text-center"
          >
            <template #body="data">
              <div v-if="data.data.unit_id == 0">
                <i
                  class="pi text-blue-400 pi-check"
                  style="font-size: 1.5rem"
                ></i>
              </div>
              <div v-else></div>
            </template>
          </Column>
          <Column
            header="Chức năng"
            class="align-items-center justify-content-center text-center"
            headerStyle="text-align:center;height:50px;max-width:150px;"
            bodyStyle="text-align:center;max-height:60px;max-width:150px"
          >
            <template #body="Email">
              <div>
                <Button
                  @click="editEmail(Email.data)"
                  class="
                    p-button-rounded p-button-secondary p-button-outlined
                    mx-1
                  "
                  type="button"
                  icon="pi pi-pencil"
                ></Button>
                <Button
                  @click="delEmail(Email.data, true)"
                  class="
                    p-button-rounded p-button-secondary p-button-outlined
                    mx-1
                  "
                  type="button"
                  icon="pi pi-trash"
                ></Button>
              </div>
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
  </div>
  <Dialog
    :header="headerDialog"
    v-model:visible="displayBasic"
    :style="{ width: '40vw' }"
  >
    <form>
      <div class="grid formgrid m-2">
        <div class="field col-12 md:col-12">
          <label class="col-3 text-left p-0"
            >Nhóm Email<span class="redsao">(*)</span></label
          >
          <InputText
            v-model="EmailGroup.email_group_name"
            spellcheck="false"
            class="col-8 ip36 px-2"
            :class="{ 'p-invalid': v$.email_group_name.$invalid && submitted }"
          />
        </div>
        <div style="display: flex" class="field col-12 md:col-12">
          <div class="col-3 text-left"></div>
          <small
            v-if="
              (v$.email_group_name.$invalid && submitted) ||
              v$.email_group_name.$pending.$response
            "
            class="col-9 p-error p-0"
          >
            <span class="col-12 p-0">{{
              v$.email_group_name.required.$message
                .replace("Value", "Tên nhóm email")
                .replace("is required", "không được để trống")
            }}</span>
          </small>
        </div>
        <div style="display: flex" class="col-12 field md:col-12">
          <div class="field col-6 md:col-6 p-0">
            <label class="col-6 text-left p-0">Số thứ tự </label>
            <InputNumber v-model="EmailGroup.is_order" class="col-6 ip36 p-0" />
          </div>
          <div class="field col-6 md:col-6 p-0">
            <label
              style="vertical-align: text-bottom"
              class="col-6 text-center p-0"
              >Trạng thái
            </label>
            <InputSwitch v-model="EmailGroup.status" class="col-6" />
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
        @click="saveEmailGroup(!v$.$invalid)"
      />
    </template>
  </Dialog>
  <Dialog
    :header="headerAddEmail"
    v-model:visible="displayEmail"
    :style="{ width: '40vw' }"
  >
    <form>
      <div class="grid formgrid m-2">
        <div class="field col-12 md:col-12">
          <label class="col-3 text-left p-0"
            >Email <span class="redsao">(*)</span></label
          >
          <InputText
            v-model="Email.email_name"
            spellcheck="false"
            class="col-8 ip36 px-2"
            :class="{
              'p-invalid': validateEmail.email_name.$invalid && submitted,
            }"
          />
        </div>
        <div style="display: flex" class="field col-12 md:col-12">
          <div class="col-3 text-left"></div>
          <small
            v-if="
              (validateEmail.email_name.$invalid && submitted) ||
              validateEmail.email_name.$pending.$response
            "
            class="col-9 p-error p-0"
          >
            <span class="col-12 p-0">{{
              validateEmail.email_name.required.$message
                .replace("Value", "Tên email")
                .replace("is required", "không được để trống")
            }}</span>
          </small>
        </div>
        <div class="field col-12 md:col-12">
          <label class="col-3 text-left p-0"
            >Họ và tên <span class="redsao">(*)</span></label
          >
          <InputText
            v-model="Email.full_name"
            spellcheck="false"
            class="col-8 ip36 px-2"
            :class="{
              'p-invalid': validateEmail.full_name.$invalid && submitted,
            }"
          />
        </div>
        <div style="display: flex" class="field col-12 md:col-12">
          <div class="col-3 text-left"></div>
          <small
            v-if="
              (validateEmail.full_name.$invalid && submitted) ||
              validateEmail.full_name.$pending.$response
            "
            class="col-9 p-error p-0"
          >
            <span class="col-12 p-0">{{
              validateEmail.full_name.required.$message
                .replace("Value", "Họ tên email")
                .replace("is required", "không được để trống")
            }}</span>
          </small>
        </div>
        <div class="field col-12 md:col-12">
          <label class="col-3 text-left p-0">Mô tả </label>
          <InputText
            v-model="Email.description"
            spellcheck="false"
            class="col-8 ip36 px-2"
          />
        </div>
        <div style="display: flex" class="col-12 field md:col-12">
          <div class="field col-6 md:col-6 p-0">
            <label class="col-6 text-left p-0">Số thứ tự </label>
            <InputNumber v-model="Email.is_order" class="col-6 ip36 p-0" />
          </div>
          <div class="field col-6 md:col-6 p-0">
            <label
              style="vertical-align: text-bottom"
              class="col-6 text-center p-0"
              >Trạng thái
            </label>
            <InputSwitch v-model="Email.status" class="col-6" />
          </div>
        </div>
      </div>
    </form>
    <template #footer>
      <Button
        label="Hủy"
        icon="pi pi-times"
        @click="closeDialogEmail"
        class="p-button-text"
      />

      <Button
        label="Lưu"
        icon="pi pi-check"
        @click="saveEmail(!validateEmail.$invalid)"
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