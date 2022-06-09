<script setup>
import { ref, inject, onMounted } from "vue";
import { required } from "@vuelidate/validators";
import { useToast } from "vue-toastification";
import { useVuelidate } from "@vuelidate/core";
import { getParent } from "../../util/function";
//init Model
const user = ref({
  FullName: "",
  STT: 1,
  Trangthai: true,
  IsAdmin: true,
  Trangthai: 1,
});
//Valid Form
const submitted = ref(false);
const rules = {
  Users_ID: {
    required,
  },
  FullName: {
    required,
  },
};
const v$ = useVuelidate(rules, user);
//Khai báo biến
const tdQuyens = [
  { value: 0, text: "Không có quyền (0)" },
  { value: 1, text: "Xem cá nhân (1)" },
  { value: 2, text: "Xem tất cả (2)" },
  { value: 3, text: "Chỉnh sửa cá nhân (3)" },
  { value: 4, text: "Chỉnh sửa tất cả (4)" },
  { value: 5, text: "Duyệt (5)" },
  { value: 6, text: "Full (6)" },
].reverse();
const store = inject("store");
const isAdd = ref(true);
const selectedKey = ref();
const selectedNodes = ref([]);
const filters = ref({});
const opition = ref({
  search: "",
  PageNo: 1,
  PageSize: 20,
  FilterUsers_ID: null,
  Users_ID: store.getters.user.Users_ID,
});
const users = ref();
const treeusers = ref();
const displayAddUser = ref(false);
const isFirst = ref(true);
let files = [];
const toast = useToast();
const swal = inject("$swal");
const axios = inject("axios"); // inject axios
const basedomainURL = baseURL;
const layout = ref("grid");
const config = {
  headers: { Authorization: `Bearer ${store.getters.token}` },
};
const tdTrangthais = ref([
  { value: 0, text: "Khoá" },
  { value: 1, text: "Kích hoạt" },
  { value: 2, text: "Đợi xác thực" },
]);
const tdRoles = ref([]);
const menuButs = ref();
const menuButMores = ref();
const itemButs = ref([
  {
    label: "Xuất Excel",
    icon: "pi pi-file-excel",
    command: (event) => {
      exportUser("ExportExcel");
    },
  },
  {
    label: "Xuất Mẫu",
    icon: "pi pi-file-excel",
    command: (event) => {
      exportUser("ExportExcelMau");
    },
  },
]);
const itemButMores = ref([
  {
    label: "Sửa User",
    icon: "pi pi-user-edit",
    command: (event) => {
      editUser(user.value);
    },
  },
  {
    label: "Phân quyền User",
    icon: "pi pi-key",
    command: (event) => {
      configRole(user.value);
    },
  },
  {
    label: "Xoá User",
    icon: "pi pi-trash",
    command: (event) => {
      delUser(user.value);
    },
  },
]);

//Khai báo function
const toggleExport = (event) => {
  menuButs.value.toggle(event);
};
const toggleMores = (event, u) => {
  user.value = u;
  menuButMores.value.toggle(event);
};
const onNodeSelect = (node) => {
  selectedNodes.value.push(node.data.User_ID);
};
const onNodeUnselect = (node) => {
  selectedNodes.value.splice(selectedNodes.value.indexOf(node.data.User_ID), 1);
};
const handleFileUpload = (event) => {
  files = event.target.files;
  var output = document.getElementById("userAnh");
  output.src = URL.createObjectURL(event.target.files[0]);
  output.onload = function () {
    URL.revokeObjectURL(output.src); // free memory
  };
};
//Show Modal
const showModalAddUser = () => {
  submitted.value = false;
  selectCapcha.value = {};
  user.value = {
    FullName: "",
    STT: users.value.length + 1,
    Trangthai: 1,
    Role_ID: "admin",
    IsAdmin: true,
    IsTarget: "_self",
  };
  displayAddUser.value = true;
};
const chonanh = (id) => {
  document.getElementById(id).click();
};
const closedisplayAddUser = () => {
  displayAddUser.value = false;
};
//Thêm sửa xoá
const onRefersh = () => {
  opition.value = {
    search: "",
    PageNo: 1,
    PageSize: 20,
    FilterUsers_ID: null,
    Users_ID: store.getters.user.Users_ID,
  };
  loadUser(true);
};
const onSearch = () => {
  loadUser(true);
};
const donvis = ref();
const treedonvis = ref();
const selectCapcha = ref();
const renderTreeDV = (data, id, name, title) => {
  let arrChils = [];
  let arrtreeChils = [];
  data
    .filter((x) => x.Capcha_ID == null)
    .forEach((m, i) => {
      m.IsOrder = i + 1;
      let om = { key: m[id], data: m };
      const rechildren = (mm, pid) => {
        let dts = data.filter((x) => x.Capcha_ID == pid);
        if (dts.length > 0) {
          if (!mm.children) mm.children = [];
          dts.forEach((em) => {
            let om1 = { key: em[id], data: em };
            rechildren(om1, em[id]);
            mm.children.push(om1);
          });
        }
      };
      rechildren(om, m[id]);
      arrChils.push(om);
      //
      om = { key: m[id], data: m[id], label: m[name] };
      const retreechildren = (mm, pid) => {
        let dts = data.filter((x) => x.Capcha_ID == pid);
        if (dts.length > 0) {
          if (!mm.children) mm.children = [];
          dts.forEach((em) => {
            let om1 = { key: em[id], data: em[id], label: em[name] };
            retreechildren(om1, em[id]);
            mm.children.push(om1);
          });
        }
      };
      retreechildren(om, m[id]);
      arrtreeChils.push(om);
    });
  arrtreeChils.unshift({ key: -1, data: -1, label: "-----Chọn " + title + "----" });
  return { arrChils: arrChils, arrtreeChils: arrtreeChils };
};
const initTudien = () => {
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "Sys_Users_ListTudien",
        par: [{ par: "Users_ID", va: opition.value.Users_ID }],
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data)[0];
      if (data.length > 0) {
        tdRoles.value = data;
      }
      data = JSON.parse(response.data.data)[1];
      if (data.length > 0) {
        let obj = renderTreeDV(data, "Donvi_ID", "TenDonvi", "đơn vị");
        donvis.value = obj.arrChils;
        treedonvis.value = obj.arrtreeChils;
      } else {
        donvis.value = [];
      }
    })
    .catch((error) => {});
};
const loadCount = () => {
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "Sys_Users_Count",
        par: [
          { par: "Search", va: opition.value.search },
          { par: "Users_ID", va: opition.value.Users_ID },
          { par: "Role_ID", va: opition.value.Role_ID },
          { par: "Donvi_ID", va: opition.value.Donvi_ID },
          { par: "Phongban_ID", va: opition.value.Phongban_ID },
          { par: "PageNo", va: opition.value.PageNo },
          { par: "PageSize", va: opition.value.PageSize },
          { par: "IsAdmin", va: opition.value.IsAdmin },
          { par: "Trangthai", va: opition.value.Trangthai },
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
    .catch((error) => {});
};
const onPage = (event) => {
  opition.value.PageNo = event.page + 1;
  opition.value.PageSize = event.rows;
  loadUser(true);
};
const goDonvi = (u) => {
  if (u) {
    opition.value.Phongban_ID = u.Phongban_ID;
    opition.value.TenDonvi = u.TenDonvi;
  } else {
    opition.value.Phongban_ID = null;
    opition.value.TenDonvi = null;
  }
  opition.value.PageNo == 1;
  loadUser(true);
};
const goRole = (u) => {
  if (u) {
    opition.value.Role_ID = u.Role_ID;
    opition.value.Role_Name = u.Role_Name;
    opition.value.Mauchu = u.Mauchu;
    opition.value.Maunen = u.Maunen;
  } else {
    opition.value.Role_ID = null;
    opition.value.Role_Name = null;
  }
  opition.value.PageNo == 1;
  loadUser(true);
};
const goTrangthai = (u) => {
  if (u) {
    opition.value.Trangthai = u.Trangthai;
    opition.value.TenTrangthai = u.TenTrangthai;
  } else {
    opition.value.Trangthai = null;
    opition.value.TenTrangthai = null;
  }
  opition.value.PageNo == 1;
  loadUser(true);
};
const resetopition = () => {
  if (opition.value.Role_ID && !opition.value.Role_Name) {
    let u = tdRoles.value.find((x) => x.Role_ID == opition.value.Role_ID);
    opition.value.Role_Name = u.Role_Name;
    opition.value.Mauchu = u.Mauchu;
    opition.value.Maunen = u.Maunen;
  }
  if (opition.value.Donvi_ID && !opition.value.TenDonvi) {
    opition.value.TenDonvi = users.value.find(
      (x) => x.Donvi_ID == opition.value.Donvi_ID
    ).TenDonvi;
  }
  if (opition.value.Trangthai && !opition.value.TenTrangthai) {
    opition.value.TenTrangthai = tdTrangthais.value.find(
      (x) => x.Trangthai == opition.value.Trangthai
    ).TenTrangthai;
  }
};
const loadUser = (rf, rfpb) => {
  resetopition();
  if (rf) {
    opition.value.loading = true;
    swal.fire({
      width: 110,
      didOpen: () => {
        swal.showLoading();
      },
    });
    if (opition.value.PageNo == 1) loadCount();
  }
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "Sys_Users_List",
        par: [
          { par: "Search", va: opition.value.search },
          { par: "Users_ID", va: opition.value.Users_ID },
          { par: "Role_ID", va: opition.value.Role_ID },
          { par: "Donvi_ID", va: opition.value.Donvi_ID },
          { par: "Phongban_ID", va: opition.value.Phongban_ID },
          { par: "PageNo", va: opition.value.PageNo },
          { par: "PageSize", va: opition.value.PageSize },
          { par: "IsAdmin", va: opition.value.IsAdmin },
          { par: "Trangthai", va: opition.value.Trangthai },
          { par: "StartDate", va: opition.value.StartDate },
          { par: "EndDate", va: opition.value.EndDate },
        ],
      },
      config
    )
    .then((response) => {
      let data = JSON.parse(response.data.data)[0];
      if (data.length > 0) {
        users.value = data;
      } else {
        users.value = [];
      }
      if (isFirst.value) isFirst.value = false;
      if (rf) {
        opition.value.loading = false;
        swal.close();
      }
      if (rfpb) {
        initUserPhongban();
      }
    })
    .catch((error) => {
      console.log(error);
      if (error && error.status === 401) {
        swal.fire({
          title: "Error!",
          text: "Mã token đã hết hạn hoặc không hợp lệ, vui lòng đăng nhập lại!",
          icon: "error",
          confirmButtonText: "OK",
        });
      }
    });
};
const editUser = (md) => {
  submitted.value = false;
  isAdd.value = false;
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  displayAddUser.value = true;
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      { proc: "Sys_Users_Get", par: [{ par: "Users_ID", va: md.Users_ID }] },
      config
    )
    .then((response) => {
      swal.close();
      let data = JSON.parse(response.data.data);
      if (data.length > 0) {
        user.value = data[0][0];
        selectCapcha.value = {};
        selectCapcha.value[user.value.Phongban_ID || "-1"] = true;
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
const handleSubmit = (isFormValid) => {
  submitted.value = true;
  if (!isFormValid) {
    return;
  }
  let keys = Object.keys(selectCapcha.value);
  user.value.Phongban_ID = keys[0];
  if (user.value.Phongban_ID == -1) {
    user.value.Phongban_ID = null;
  }
  const result = getParent(treedonvis.value, user.value.Phongban_ID, "key");
  user.value.Donvi_ID = result.key;
  addUser();
};
const addUser = () => {
  let formData = new FormData();
  for (var i = 0; i < files.length; i++) {
    let file = files[i];
    formData.append("anh", file);
  }
  formData.append("model", JSON.stringify(user.value));
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  axios({
    method: isAdd.value == false ? "put" : "post",
    url: baseURL + `/api/Users/${isAdd.value == false ? "Update_Users" : "Add_Users"}`,
    data: formData,
    headers: {
      Authorization: `Bearer ${store.getters.token}`,
    },
  })
    .then((response) => {
      if (response.data.err != "1") {
        swal.close();
        toast.success("Cập nhật User thành công!");
        loadUser();
        closedisplayAddUser();
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

const delUser = (md) => {
  swal
    .fire({
      title: "Thông báo",
      text: "Bạn có muốn xoá menu này không!",
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
          .delete(baseURL + "/api/Users/Del_Users", {
            headers: { Authorization: `Bearer ${store.getters.token}` },
            data: md == null ? [md.User_ID] : selectedNodes.value,
          })
          .then((response) => {
            swal.close();
            if (response.data.err != "1") {
              swal.close();
              toast.success("Xoá User thành công!");
              loadUser();
              if (!md) selectedNodes.value = [];
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

const exportUser = (method) => {
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  axios
    .post(
      baseURL + "/api/Excel/" + method,
      {
        excelname: "DANH SÁCH NGƯỜI DÙNG",
        proc: "Sys_Users_ListExport",
        par: [
          { par: "Search", va: opition.value.search },
          { par: "Users_ID", va: opition.value.Users_ID },
          { par: "Role_ID", va: opition.value.Role_ID },
          { par: "IsAdmin", va: opition.value.IsAdmin },
          { par: "Trangthai", va: opition.value.Trangthai },
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

const displayConfigRole = ref(false);
const modules = ref([]);
const renderTree = (data) => {
  let arrChils = [];
  data
    .filter((x) => x.Capcha_ID == null)
    .forEach((m, i) => {
      m.IsOrder = i + 1;
      let om = { key: m.Module_ID, data: m };
      const rechildren = (mm, Module_ID) => {
        let dts = data.filter((x) => x.Capcha_ID == Module_ID);
        if (dts.length > 0) {
          if (!mm.children) mm.children = [];
          dts.forEach((em) => {
            let om1 = { key: em.Module_ID, data: em };
            rechildren(om1, em.Module_ID);
            mm.children.push(om1);
          });
        }
      };
      rechildren(om, m.Module_ID);
      arrChils.push(om);
    });
  modules.value = arrChils;
};
const configRole = (md) => {
  modules.value.forEach((el) => {
    el.data.IsQuyen = null;
    if (el.children) {
      el.children.forEach((element) => {
        element.data.IsQuyen = null;
      });
    }
  });
  //Config quyền
  opition.value.moduleloading = true;
  displayConfigRole.value = true;
  axios
    .post(
      baseURL + "/api/Proc/CallProc",
      {
        proc: "ModuleListByUser",
        par: [
          { par: "Role_ID ", va: md.Role_ID },
          { par: "Users_ID ", va: md.Users_ID },
        ],
      },
      config
    )
    .then((response) => {
      opition.value.moduleloading = false;
      let data = JSON.parse(response.data.data)[0];
      if (data.length > 0) {
        data
          .filter((x) => x.IsQuyen)
          .forEach((r) => {
            let ds = r.IsQuyen.toString().split("");
            var arrs = [];
            ds.forEach((e) => {
              arrs.push(parseInt(e));
            });
            r.IsQuyen = arrs;
          });
        renderTree(data);
      } else {
        modules.value = [];
      }
    })
    .catch((error) => {
      opition.value.moduleloading = false;
      if (error && error.status === 401) {
        swal.fire({
          title: "Error!",
          text: "Mã token đã hết hạn hoặc không hợp lệ, vui lòng đăng nhập lại!",
          icon: "error",
          confirmButtonText: "OK",
        });
      }
    });
};
const closedisplayConfigRole = () => {
  displayConfigRole.value = false;
};
const changeQuyen = (md) => {
  if (md.children) {
    md.children.forEach((element) => {
      element.data.IsQuyen = md.data.IsQuyen;
    });
  }
};
const addConfigRole = () => {
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  let mdmodules = [];
  modules.value.forEach((element) => {
    mdmodules.push({
      RoleModule_ID: element.data.RoleModule_ID || -1,
      Role_ID: element.data.Role_ID,
      Users_ID: element.data.Users_ID,
      Module_ID: element.data.Module_ID,
      IsCap: element.data.IsCap,
      IsQuyen: element.data.IsQuyen
        ? element.data.IsQuyen.join("")
        : element.data.IsQuyen,
    });
    if (element.children && element.children.length > 0) {
      element.children.forEach((ec) => {
        mdmodules.push({
          RoleModule_ID: ec.data.RoleModule_ID || -1,
          Role_ID: ec.data.Role_ID,
          Users_ID: element.data.Users_ID,
          Module_ID: ec.data.Module_ID,
          IsCap: ec.data.IsCap,
          IsQuyen: ec.data.IsQuyen ? ec.data.IsQuyen.join("") : ec.data.IsQuyen,
        });
      });
    }
  });
  axios({
    method: "post",
    url: baseURL + "/api/Roles/Add_RoleModules",
    data: mdmodules,
    headers: {
      Authorization: `Bearer ${store.getters.token}`,
    },
  })
    .then((response) => {
      swal.close();
      if (response.data.err != "1") {
        swal.close();
        toast.success("Phân quyền user thành công!");
        displayConfigRole.value = false;
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
};
const displayPhongban = ref(false);
const showPhongban = () => {
  displayPhongban.value = !displayPhongban.value;
  if (displayPhongban.value) {
    layout.value="list";
    opition.value.PageSize = opition.value.totalRecords;
    opition.value.PageNo = 1;
    loadUser(true, true);
  }
};
const initUserPhongban = () => {
  const ipb = (dv) => {
    if (dv.children) {
      dv.children.forEach((el) => {
        ipb(el);
      });
    }
    if (dv.data.IsDonvi == false) {
      let us = users.value.filter((x) => x.Phongban_ID == dv.key);
      if (us.length > 0) {
        if (!dv.children) dv.children = [];
        //dv.children = dv.children.concat(us);
        dv.children = dv.children.concat({
          data: { TenDonvi: "Danh sách người dùng (" + us.length + ")", IsDonvi: false },
          users: us,
        });
        //dv.users = us;
      }
    }
  };
  donvis.value.forEach((dv) => {
    ipb(dv);
  });
  console.log(donvis.value);
};
onMounted(() => {
  //init
  loadUser(true);
  initTudien();
});
</script>
<template>
  <div class="main-layout flex-grow-1 p-2" v-if="store.getters.islogin">
    <TreeTable
      v-if="displayPhongban"
      :value="donvis"
      :loading="opition.loading"
      class="p-treetable-sm e-sm"
      :scrollable="true"
      scrollHeight="flex"
    >
      <template #header>
        <h3 class="module-title mt-0 ml-1 mb-2">
          <i class="pi pi-users"></i> Người dùng
          <span v-if="opition.totalRecords > 0">({{ opition.totalRecords }})</span>
          <Chip
            class="custom-chip ml-2 mr-1"
            @remove="goRole()"
            v-if="opition.Role_ID"
            :label="opition.Role_Name"
            :style="{
              background: opition.Maunen,
              color: opition.Mauchu,
            }"
            removable
          />
          <Chip
            class="custom-chip chippb ml-2 mr-1"
            @remove="goDonvi()"
            v-if="opition.Phongban_ID"
            :label="opition.TenDonvi"
            removable
          />
          <Chip
            class="custom-chip ml-2 mr-1"
            v-bind:class="
              'chip-' +
              (opition.Trangthai == 0
                ? 'danger'
                : opition.Trangthai == 1
                ? 'success'
                : 'warning')
            "
            @remove="goTrangthai()"
            v-if="opition.Trangthai"
            :label="opition.TenTrangthai"
            removable
          />
        </h3>
        <Toolbar class="w-full custoolbar">
          <template #start>
            <Dropdown
              :showClear="true"
              v-model="opition.Role_ID"
              :options="tdRoles"
              optionLabel="Role_Name"
              optionValue="Role_ID"
              placeholder="Lọc theo nhóm người dùng"
              class="p-dropdown-sm"
              @change="loadUser(true)"
            />
            <span class="p-input-icon-left ml-2">
              <i class="pi pi-search" />
              <InputText
                type="text"
                class="p-inputtext-sm"
                spellcheck="false"
                v-model="opition.search"
                placeholder="Tìm kiếm"
                v-on:keyup.enter="onSearch"
              />
            </span>
          </template>

          <template #end>
            <DataViewLayoutOptions v-model="layout" />
            <Button
              icon="pi pi-list"
              v-tooltip.left="'Hiển thị phòng ban'"
              v-bind:class="
                'ml-2 p-button-sm p-button-' + (displayPhongban ? 'primary' : 'secondary')
              "
              @click="showPhongban"
            />
            <Button
              class="mr-2 ml-2 p-button-sm p-button-outlined p-button-secondary"
              icon="pi pi-refresh"
              @click="onRefersh"
            />
            <Button
              label="Xoá"
              icon="pi pi-trash"
              class="mr-2 p-button-danger"
              v-if="selectedNodes.length > 0"
              @click="delUser"
            />
            <Button
              label="Export"
              icon="pi pi-file-excel"
              class="mr-2 p-button-sm p-button-outlined p-button-secondary"
              @click="toggleExport"
              aria-haspopup="true"
              aria-controls="overlay_Export"
            />
            <Menu id="overlay_Export" ref="menuButs" :model="itemButs" :popup="true" />
            <Button
              label="Thêm User"
              icon="pi pi-plus"
              class="mr-2 p-button-sm"
              @click="showModalAddUser"
            />
          </template>
        </Toolbar>
      </template>
      <Column field="TenDonvi" header="Tên đơn vị" :expander="true">
        <template #body="md">
          <div class="w-full" v-if="md.node.data">
            <div :class="'block font-bold mb-2 donvi' + md.node.data.IsDonvi">
              {{ md.node.data.TenDonvi }}
            </div>
            <div
              v-if="md.node.users"
              :style="
                'background-color: #eee; display: ' +
                (layout == 'grid' ? 'flex' : 'block')
              "
            >
              <div
                v-if="layout == 'grid'"
                class="col-12 md:col-3 p-2"
                v-for="u in md.node.users"
                v-bind:key="u.Users_ID"
              >
                <Card class="no-paddcontent">
                  <template #title>
                    <div style="position: relative">
                      <div class="align-items-center justify-content-center text-center">
                        <Avatar
                          v-bind:label="u.Avartar ? '' : u.FullName.substring(0, 1)"
                          v-bind:image="basedomainURL + u.Avartar"
                          style="
                            background-color: #2196f3;
                            color: #ffffff;
                            width: 8rem;
                            height: 8rem;
                          "
                          class="mr-2"
                          size="xlarge"
                          shape="circle"
                        />
                      </div>
                      <Button
                        style="position: absolute; right: 0px; top: 0px"
                        icon="pi pi-ellipsis-h"
                        class="p-button-rounded p-button-text ml-2"
                        @click="toggleMores($event, u)"
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
                  <template #content>
                    <div class="text-center">
                      <Button
                        class="p-button-text m-auto block"
                        style="color: inherit"
                        @click="editUser(u)"
                        ><h3 class="m-0">
                          {{ u.FullName }}
                        </h3></Button
                      >
                      <Chip
                        @click="goDonvi(u)"
                        class="m-1 chippb p-ripple"
                        v-ripple
                        :label="u.TenDonvi"
                      ></Chip>
                      <div class="mb-1">
                        <Chip
                          @click="goRole(u)"
                          v-ripple
                          class="p-ripple"
                          :style="{
                            background: u.Maunen,
                            color: u.Mauchu,
                          }"
                          v-bind:label="u.Role_Name"
                        />
                      </div>
                      <Button
                        v-bind:label="u.TenTrangthai"
                        @click="goTrangthai(u)"
                        v-bind:class="
                          'p-button-sm p-button-' +
                          (u.Trangthai == 0
                            ? 'danger'
                            : u.Trangthai == 1
                            ? 'success'
                            : 'warning') +
                          ' p-button-rounded'
                        "
                      />
                    </div>
                  </template>
                </Card>
              </div>
              <div
                v-if="layout == 'list'"
                v-for="u in md.node.users"
                v-bind:key="u.Users_ID"
                class="p-2 w-full"
                style="background-color: #fff"
              >
                <div class="flex align-items-center justify-content-center">
                  <Avatar
                    v-bind:label="u.Avartar ? '' : u.FullName.substring(0, 1)"
                    v-bind:image="basedomainURL + u.Avartar"
                    style="background-color: #2196f3; color: #ffffff"
                    class="mr-2"
                    size="xlarge"
                    shape="circle"
                  />
                  <div class="flex flex-column flex-grow-1">
                    <Button
                      class="p-button-text p-0"
                      style="color: inherit; padding: 0 !important"
                      @click="editUser(u)"
                      ><h3 class="mb-1 mt-0">{{ u.FullName }}</h3></Button
                    >
                    <i style="font-size: 10pt; color: #999"
                      >{{ u.Users_ID }} | {{ u.Phone }}</i
                    >
                    <i style="font-size: 10pt; color: #999">{{ u.Email }}</i>
                  </div>
                  <Chip
                    @click="goDonvi(u)"
                    class="ml-2 mr-2 chippb"
                    :label="u.TenDonvi"
                  ></Chip>
                  <Chip
                    @click="goRole(u)"
                    class="ml-2 mr-2"
                    :style="{
                      background: u.Maunen,
                      color: u.Mauchu,
                    }"
                    v-bind:label="u.Role_Name"
                  />
                  <div
                    v-bind:class="'rolefalse'"
                    style="background-color: #eee; font-size: 10pt"
                  >
                    {{ u.Ngay }}
                  </div>
                  <Button
                    @click="goTrangthai(u)"
                    v-bind:label="u.TenTrangthai"
                    v-bind:class="
                      'ml-2 mr-2 p-button-sm p-button-' +
                      (u.Trangthai == 0
                        ? 'danger'
                        : u.Trangthai == 1
                        ? 'success'
                        : 'warning') +
                      ' p-button-rounded'
                    "
                  />
                  <Button
                    icon="pi pi-ellipsis-h"
                    class="p-button-outlined p-button-secondary ml-2"
                    @click="toggleMores($event, u)"
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
              </div>
            </div>
          </div>
        </template>
      </Column>
      <template #empty>
        <div
          class="m-auto align-items-center justify-content-center p-4 text-center"
          v-if="!isFirst"
        >
          <img src="../../assets/background/nodata.png" height="144" />
          <h3 class="m-1">Không có dữ liệu</h3>
        </div>
      </template>
    </TreeTable>
    <DataView
      v-if="!displayPhongban"
      class="w-full h-full e-sm flex flex-column"
      :lazy="true"
      :value="users"
      :layout="layout"
      :loading="opition.loading"
      :paginator="opition.totalRecords > opition.PageSize"
      :rows="opition.PageSize"
      :totalRecords="opition.totalRecords"
      :pageLinkSize="opition.PageSize"
      @page="onPage($event)"
      paginatorTemplate="FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink CurrentPageReport RowsPerPageDropdown"
      :rowsPerPageOptions="[8, 12, 20, 50, 100]"
      currentPageReportTemplate=""
      responsiveLayout="scroll"
      :scrollable="true"
    >
      <template #header>
        <h3 class="module-title mt-0 ml-1 mb-2">
          <i class="pi pi-users"></i> Người dùng
          <span v-if="opition.totalRecords > 0">({{ opition.totalRecords }})</span>
          <Chip
            class="custom-chip ml-2 mr-1"
            @remove="goRole()"
            v-if="opition.Role_ID"
            :label="opition.Role_Name"
            :style="{
              background: opition.Maunen,
              color: opition.Mauchu,
            }"
            removable
          />
          <Chip
            class="custom-chip chippb ml-2 mr-1"
            @remove="goDonvi()"
            v-if="opition.Phongban_ID"
            :label="opition.TenDonvi"
            removable
          />
          <Chip
            class="custom-chip ml-2 mr-1"
            v-bind:class="
              'chip-' +
              (opition.Trangthai == 0
                ? 'danger'
                : opition.Trangthai == 1
                ? 'success'
                : 'warning')
            "
            @remove="goTrangthai()"
            v-if="opition.Trangthai"
            :label="opition.TenTrangthai"
            removable
          />
        </h3>
        <Toolbar class="w-full custoolbar">
          <template #start>
            <Dropdown
              :showClear="true"
              v-model="opition.Role_ID"
              :options="tdRoles"
              optionLabel="Role_Name"
              optionValue="Role_ID"
              placeholder="Lọc theo nhóm người dùng"
              class="p-dropdown-sm"
              @change="loadUser(true)"
            />
            <span class="p-input-icon-left ml-2">
              <i class="pi pi-search" />
              <InputText
                type="text"
                class="p-inputtext-sm"
                spellcheck="false"
                v-model="opition.search"
                placeholder="Tìm kiếm"
                v-on:keyup.enter="onSearch"
              />
            </span>
          </template>

          <template #end>
            <DataViewLayoutOptions v-model="layout" />
            <Button
              icon="pi pi-list"
              v-tooltip.left="'Hiển thị phòng ban'"
              v-bind:class="
                'ml-2 p-button-sm p-button-' + (displayPhongban ? 'primary' : 'secondary')
              "
              @click="showPhongban"
            />
            <Button
              class="mr-2 ml-2 p-button-sm p-button-outlined p-button-secondary"
              icon="pi pi-refresh"
              @click="onRefersh"
            />
            <Button
              label="Xoá"
              icon="pi pi-trash"
              class="mr-2 p-button-danger"
              v-if="selectedNodes.length > 0"
              @click="delUser"
            />
            <Button
              label="Export"
              icon="pi pi-file-excel"
              class="mr-2 p-button-sm p-button-outlined p-button-secondary"
              @click="toggleExport"
              aria-haspopup="true"
              aria-controls="overlay_Export"
            />
            <Menu id="overlay_Export" ref="menuButs" :model="itemButs" :popup="true" />
            <Button
              label="Thêm User"
              icon="pi pi-plus"
              class="mr-2 p-button-sm"
              @click="showModalAddUser"
            />
          </template>
        </Toolbar>
      </template>
      <template #grid="slotProps">
        <div class="col-12 md:col-3 p-2">
          <Card class="no-paddcontent">
            <template #title>
              <div style="position: relative">
                <div class="align-items-center justify-content-center text-center">
                  <Avatar
                    v-bind:label="
                      slotProps.data.Avartar
                        ? ''
                        : slotProps.data.FullName.substring(0, 1)
                    "
                    v-bind:image="basedomainURL + slotProps.data.Avartar"
                    style="
                      background-color: #2196f3;
                      color: #ffffff;
                      width: 8rem;
                      height: 8rem;
                    "
                    class="mr-2"
                    size="xlarge"
                    shape="circle"
                  />
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
            <template #content>
              <div class="text-center">
                <Button
                  class="p-button-text m-auto block"
                  style="color: inherit"
                  @click="editUser(slotProps.data)"
                  ><h3 class="m-0">
                    {{ slotProps.data.FullName }}
                  </h3></Button
                >
                <Chip
                  @click="goDonvi(slotProps.data)"
                  class="m-1 chippb p-ripple"
                  v-ripple
                  :label="slotProps.data.TenDonvi"
                ></Chip>
                <div class="mb-1">
                  <Chip
                    @click="goRole(slotProps.data)"
                    v-ripple
                    class="p-ripple"
                    :style="{
                      background: slotProps.data.Maunen,
                      color: slotProps.data.Mauchu,
                    }"
                    v-bind:label="slotProps.data.Role_Name"
                  />
                </div>
                <Button
                  v-bind:label="slotProps.data.TenTrangthai"
                  @click="goTrangthai(slotProps.data)"
                  v-bind:class="
                    'p-button-sm p-button-' +
                    (slotProps.data.Trangthai == 0
                      ? 'danger'
                      : slotProps.data.Trangthai == 1
                      ? 'success'
                      : 'warning') +
                    ' p-button-rounded'
                  "
                />
              </div>
            </template>
          </Card>
        </div>
      </template>
      <template #list="slotProps">
        <div class="p-2 w-full" style="background-color: #fff">
          <div class="flex align-items-center justify-content-center">
            <Avatar
              v-bind:label="
                slotProps.data.Avartar ? '' : slotProps.data.FullName.substring(0, 1)
              "
              v-bind:image="basedomainURL + slotProps.data.Avartar"
              style="background-color: #2196f3; color: #ffffff"
              class="mr-2"
              size="xlarge"
              shape="circle"
            />
            <div class="flex flex-column flex-grow-1">
              <Button
                class="p-button-text p-0"
                style="color: inherit; padding: 0 !important"
                @click="editUser(slotProps.data)"
                ><h3 class="mb-1 mt-0">{{ slotProps.data.FullName }}</h3></Button
              >
              <i style="font-size: 10pt; color: #999"
                >{{ slotProps.data.Users_ID }} | {{ slotProps.data.Phone }}</i
              >
              <i style="font-size: 10pt; color: #999">{{ slotProps.data.Email }}</i>
            </div>
            <Chip
              @click="goDonvi(slotProps.data)"
              class="ml-2 mr-2 chippb"
              :label="slotProps.data.TenDonvi"
            ></Chip>
            <Chip
              @click="goRole(slotProps.data)"
              class="ml-2 mr-2"
              :style="{
                background: slotProps.data.Maunen,
                color: slotProps.data.Mauchu,
              }"
              v-bind:label="slotProps.data.Role_Name"
            />
            <div
              v-bind:class="'rolefalse'"
              style="background-color: #eee; font-size: 10pt"
            >
              {{ slotProps.data.Ngay }}
            </div>
            <Button
              @click="goTrangthai(slotProps.data)"
              v-bind:label="slotProps.data.TenTrangthai"
              v-bind:class="
                'ml-2 mr-2 p-button-sm p-button-' +
                (slotProps.data.Trangthai == 0
                  ? 'danger'
                  : slotProps.data.Trangthai == 1
                  ? 'success'
                  : 'warning') +
                ' p-button-rounded'
              "
            />
            <Button
              icon="pi pi-ellipsis-h"
              class="p-button-outlined p-button-secondary ml-2"
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
        </div>
      </template>
      <template #empty>
        <div
          class="align-items-center justify-content-center p-4 text-center"
          v-if="!isFirst"
        >
          <img src="../../assets/background/nodata.png" height="144" />
          <h3 class="m-1">Không có dữ liệu</h3>
        </div>
      </template>
    </DataView>
  </div>
  <Dialog
    header="Cập nhật User"
    v-model:visible="displayAddUser"
    :style="{ width: '760px', zIndex: 1000 }"
    :maximizable="true"
    :autoZIndex="false"
    :modal="true"
  >
    <form @submit.prevent="handleSubmit(!v$.$invalid)">
      <div class="grid formgrid m-2">
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left">Mã <span class="redsao">(*)</span></label>
          <InputText
            spellcheck="false"
            v-bind:disabled="!isAdd"
            class="col-10 ip36"
            v-model="user.Users_ID"
            :class="{ 'p-invalid': v$.Users_ID.$invalid && submitted }"
          />
        </div>
        <small
          v-if="(v$.Users_ID.$invalid && submitted) || v$.Users_ID.$pending.$response"
          class="col-10 p-error"
        >
          <div class="field col-12 md:col-12">
            <label class="col-2 text-left"></label>
            <span class="col-10 pl-3">{{
              v$.Users_ID.required.$message
                .replace("Value", "Mã người dùng")
                .replace("is required", "không được để trống")
            }}</span>
          </div></small
        >
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left">Tên <span class="redsao">(*)</span></label>
          <InputText
            spellcheck="false"
            class="col-10 ip36"
            v-model="user.FullName"
            :class="{ 'p-invalid': v$.FullName.$invalid && submitted }"
          />
        </div>
        <small
          v-if="(v$.FullName.$invalid && submitted) || v$.FullName.$pending.$response"
          class="col-10 p-error"
        >
          <div class="field col-12 md:col-12">
            <label class="col-2 text-left"></label>
            <span class="col-10 pl-3">{{
              v$.FullName.required.$message
                .replace("Value", "Tên người dùng")
                .replace("is required", "không được để trống")
            }}</span>
          </div></small
        >
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left">Mật khẩu</label>
          <Password v-model="user.IsPassWord" toggleMask>
            <template #header>
              <h6>Chọn mật khẩu</h6>
            </template>
            <template #footer="sp">
              {{ sp.level }}
              <Divider />
              <p class="mt-2">Gợi ý</p>
              <ul class="pl-2 ml-2 mt-0" style="line-height: 1.5">
                <li>Có ít nhất 1 chữ thường</li>
                <li>Có ít nhất 1 chữ hoa</li>
                <li>Có ít nhất 1 ký tự số</li>
                <li>Tối thiểu 8 ký tự</li>
              </ul>
            </template>
          </Password>
        </div>
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left">Nhóm</label>
          <Dropdown
            class="col-10"
            v-model="user.Role_ID"
            :options="tdRoles"
            optionLabel="Role_Name"
            optionValue="Role_ID"
            placeholder="Chọn nhóm"
          />
        </div>
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left">Đơn vị</label>
          <TreeSelect
            class="col-10"
            v-model="selectCapcha"
            :options="treedonvis"
            :showClear="true"
            placeholder=""
            optionLabel="data.TenDonvi"
            optionValue="data.Donvi_ID"
          ></TreeSelect>
        </div>
        <div class="col-8">
          <div class="field">
            <label class="col-3 text-left">SĐT</label>
            <InputText class="col-8 ip36" spellcheck="false" v-model="user.Phone" />
          </div>
          <div class="field">
            <label class="col-3 text-left">Email</label>
            <InputText class="col-8 ip36" spellcheck="false" v-model="user.Email" />
          </div>
          <div class="field">
            <label class="col-3 text-left">Trạng thái</label>
            <Dropdown
              class="col-8"
              v-model="user.Trangthai"
              :options="tdTrangthais"
              optionLabel="text"
              optionValue="value"
              placeholder="Chọn trạng thái"
            />
          </div>
        </div>
        <div class="col-4">
          <div class="field">
            <label class="col-12 text-rigth">Ảnh</label>
            <div class="inputanh" @click="chonanh('AnhUser')">
              <img
                id="userAnh"
                v-bind:src="
                  user.Avartar
                    ? basedomainURL + user.Avartar
                    : '/src/assets/image/noimg.jpg'
                "
              />
            </div>
            <input
              class="ipnone"
              id="AnhUser"
              type="file"
              accept="image/*"
              @change="handleFileUpload"
            />
          </div>
        </div>
        <div class="field col-12 md:col-12">
          <label class="col-2 text-left">STT</label>
          <InputNumber class="col-2 ip36 p-0" v-model="user.STT" />

          <label style="vertical-align: text-bottom" class="col-4 text-right"
            >Admin</label
          >
          <InputSwitch v-model="user.IsAdmin" />
        </div>
      </div>
    </form>
    <template #footer>
      <Button
        label="Huỷ"
        icon="pi pi-times"
        @click="closedisplayAddUser"
        class="p-button-raised p-button-secondary"
      />
      <Button label="Cập nhật" icon="pi pi-save" @click="handleSubmit(!v$.$invalid)" />
    </template>
  </Dialog>

  <Dialog
    header="Phân quyền cho người dùng"
    v-model:visible="displayConfigRole"
    :style="{ width: '850px', zIndex: 1000 }"
    :maximizable="true"
    :autoZIndex="false"
    :modal="true"
  >
    <TreeTable
      :value="modules"
      :loading="opition.moduleloading"
      :showGridlines="true"
      filterMode="lenient"
      class="p-treetable-sm e-sm"
      :paginator="modules && modules.length > 20"
      :rows="20"
      :scrollable="true"
      scrollHeight="flex"
    >
      <template #header>
        <h3 class="module-title mt-0 ml-1 mb-2">
          <i class="pi pi-microsoft"></i> Module chức năng
        </h3>
      </template>
      <Column
        field="Icon"
        :expander="true"
        header=""
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:100px"
        bodyStyle="text-align:center;max-width:100px"
      >
        <template #body="md">
          <i v-bind:class="md.node.data.Icon"></i>
        </template>
      </Column>
      <Column field="Module_Name" header="Tên Menu"> </Column>
      <Column
        field="IsQuyen"
        header="Mã Quyền"
        class="align-items-center justify-content-center text-center"
        headerStyle="text-align:center;max-width:150px"
        bodyStyle="max-width:150px"
      >
        <template #body="md">
          <b v-if="md.node.data.IsQuyen">{{ md.node.data.IsQuyen.join("") }}</b>
        </template>
      </Column>
      <Column
        headerClass="align-items-center justify-content-center text-center"
        header="Quyền"
        headerStyle="text-align:center;width:250px"
        bodyStyle="text-align:center;width:250px"
      >
        <template #body="md">
          <MultiSelect
            v-model="md.node.data.IsQuyen"
            @change="changeQuyen(md.node)"
            :style="{ width: '250px' }"
            id="overlay_Quyen"
            ref="menuQuyen"
            :popup="true"
            :options="tdQuyens"
            optionLabel="text"
            optionValue="value"
            placeholder="Chọn quyền"
          />
        </template>
      </Column>
    </TreeTable>
    <template #footer>
      <Button
        label="Huỷ"
        icon="pi pi-times"
        @click="closedisplayConfigRole"
        class="p-button-raised p-button-secondary"
      />
      <Button label="Cập nhật" icon="pi pi-save" @click="addConfigRole" />
    </template>
  </Dialog>
</template>
<style scoped>
.p-chip.custom-chip {
  background: var(--primary-color);
  color: var(--primary-color-text);
  font-size: 00.875rem;
}
.p-chip.chip-success {
  background: #689f38;
}
.p-chip.chip-danger {
  background: #d32f2f;
}
.p-chip.chip-warning {
  background: #fbc02d;
}
.chippb {
  background-color: #4285f4;
  color: #fff;
  font-size: 0.875rem;
}
.ipnone {
  display: none;
}
.inputanh {
  border: 1px solid #ccc;
  width: 96px;
  height: 96px;
  cursor: pointer;
  padding: 1px;
}
.inputanh img {
  object-fit: cover;
  width: 100%;
  height: 100%;
}
.roletrue {
  background-color: orange;
  padding: 5px 10px;
  margin: 5px auto;
  width: max-content;
  border-radius: 5px;
  margin-bottom: 10px;
  height: fit-content;
  color: #fff;
  font-size: 0.875rem;
}
.rolefalse {
  background-color: #eee;
  padding: 5px 10px;
  margin: 5px auto;
  width: max-content;
  border-radius: 5px;
  margin-bottom: 10px;
  height: fit-content;
  font-size: 0.875rem;
}
</style>
