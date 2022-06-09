<script setup>
//Import
import { useToast } from "vue-toastification";
import { inject, onMounted, ref } from "vue";

const axios = inject("axios");
const swal = inject("$swal");
//Khai báo biến
const store = inject("store");
const storetheme = inject("storetheme");

const setTheme = (name) => {
  storetheme.value = name;
  localStorage.setItem("storetheme", name);
  visibleSidebarRight.value = false;
};
const basedomainURL = baseURL;
let fnames = store.getters.user.FullName.split(" ");
let FName = fnames[fnames.length - 1].substring(0, 1);

const menu = ref();
const toast = useToast();
const langlists = ref();
const visibleSidebarRight = ref(false);
const items = ref([
  { label: "Cài đặt", icon: "pi pi-fw pi-cog", to: "/options" },
  {
    label: "Đăng xuất",
    icon: "pi pi-fw pi-power-off",
    command: (event) => {
      store.commit("gologout");
    },
  },
]);
const config = {
  headers: { Authorization: `Bearer ${store.getters.token}` },
};
const onSelectedLang = (id, islogo) => {
  store.commit("setUrlLang", islogo);
   store.commit("setLangId", id);
  let data = {
    IntID: id,
    TextID: id + "",
    BitMain: true,
  };

  axios
    .put(baseURL + "/api/CMS_Lang/Update_IsMainLang", data, config)
    .then((response) => {
      if (response.data.err != "1") {
        swal.close();

        isShowLang.value = false;
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
//Lấy danh sách ngôn ngữ
const loadLang = () => {
  // axios
  //   .post(
  //     baseURL + "/api/Proc/CallProc",
  //     {
  //       proc: "SQL_Lang_ListTrangthai",
  //     },
  //     config
  //   )
  //   .then((response) => {
  //     let data = JSON.parse(response.data.data)[0];

  //     // if (isFirst.value) isFirst.value = false;
  //     langlists.value = data;
  //     data.forEach((item) => {
  //       if (item.Ismain) {
  //         store.commit("setUrlLang", item.Islogo);
  //          store.commit("setLangId", item.ID);
  //       }
  //     });
  //   })
  //   .catch((error) => {
  //     toast.error("Tải dữ liệu không thành công!");

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
};
//
const logout = () => {
  swal
    .fire({
      title: "Thông báo",
      text: "Bạn có muốn đăng xuất khỏi tài khoản này không!",
      icon: "warning",
      showCancelButton: true,
      confirmButtonColor: "#3085d6",
      cancelButtonColor: "#d33",
      confirmButtonText: "Có",
      cancelButtonText: "Không",
    })
    .then((result) => {
      if (result.isConfirmed) {
        store.commit("gologout");
      }
    });
};
const isShowLang = ref(false);
const toggle = (event) => {
  //storetheme.value="bootstrap4-dark-blue";
  menu.value.toggle(event);
};
const toggleLang = () => {
  if (isShowLang.value) {
    isShowLang.value = false;
  } else {
    isShowLang.value = true;
  }
};
//Vue App
onMounted(() => {
  loadLang();
  return {
    menu,
    items,
  };
});
</script>
<template>
  <div class="flex flex-row shadow-3 header-bar">
    <div class="flex flex-row flex-grow-1 headerbar align-items-center">
      <div class="ml-2 mr-2">
        <img src="../../assets/image/cms.png" height="40" />
      </div>
      <h2 style="font-size: 1.1rem; color: #005a9e; font-weight: bold">
        HỆ QUẢN TRỊ NỘI DUNG CMS
      </h2>
    </div>
    <div
      class="
        flex flex-none
        align-items-center
        justify-content-center
        btn-togglang
      "
    >
      <Button
        type="button"
        class="shadow-none p-button-text r-4 p-ripple"
        @click="toggleLang"
      >
        <!-- <img
          class="d-img-lang"
          style="object-fit: cover"
          :src="basedomainURL + store.state.urlLang"
        /> -->
      </Button>
      <div class="d-menu-lang" v-if="isShowLang">
        <ScrollPanel style="width: 100%; height: 200px">
          <table class="m-table-lang">
            <tr
              @click="onSelectedLang(item.ID, item.Islogo)"
              v-for="item in langlists"
              :key="item.ID"
            >
              <td>
                <!-- <img class="d-img-lang" :src="basedomainURL + item.Islogo" /> -->
              </td>
              <td>{{ item.Name }}</td>
            </tr>
          </table>
        </ScrollPanel>
      </div>
      <i
        v-ripple
        class="pi pi-bell mr-4 p-text-secondary"
        style="font-size: 1.5rem"
        v-badge="2"
      ></i>
      <Button
        type="button"
        class="shadow-none p-button-text p-1 p-ripple"
        @click="toggle"
        aria-haspopup="true"
        aria-controls="overlay_menu"
      >
        <Avatar
          v-ripple
          v-bind:image="basedomainURL + store.getters.user.Avartar"
          v-bind:label="store.getters.user.Avartar ? '' : FName"
          style="background-color: #2196f3; color: #ffffff"
          shape="circle"
        />
      </Button>

      <Menu id="overlay_menu" ref="menu" :model="items" :popup="true" />
    </div>
  </div>
  <Sidebar
    v-model:visible="visibleSidebarRight"
    :baseZIndex="1000"
    style="width: 360px"
    position="right"
  >
    <h2 class="align-items-center justify-content-center">
      Cấu hình Theme
      <Button
        icon="pi pi-palette"
        @click="setTheme('saga-blue')"
        class="p-button-rounded"
      />
    </h2>

    <div v-for="(value, name) in skin" :key="name">
      <h5>{{ name }}</h5>
      <div class="grid col-12">
        <div
          class="col-3 align-items-center justify-content-center text-center"
          v-for="item in value"
          :key="item.name"
          @click="setTheme(item.name)"
        >
          <Avatar class="divskin" v-bind:image="item.icon" size="xlarge" />
          <h5>{{ item.title }}</h5>
        </div>
      </div>
    </div>
  </Sidebar>
</template>
<style scoped>
.headerbar {
  background-color: #fff;
  height: 50px;
}
.divskin:hover {
  cursor: pointer;
  background-color: #eee;
}
.d-img-lang {
  width: 32px;
  height: 32px;
  border-radius: 50%;
}
.btn-togglang {
  position: relative;
}
.d-menu-lang {
  width: 200px;
  position: absolute;
  top: 100%;
  right: 50%;
  background-color: rgb(255, 255, 255);
  z-index: 1000;
  padding: 0px 0px 12px 12px;
  border-radius: 0px 0px 12px 12px;
}

.m-table-lang tr {
  width: 100%;
  padding: 6px 0px 6px 0px;
}
.m-table-lang tr td {
  padding: 6px 6px 6px 0px;
  border: unset;
}
.m-table-lang tr:hover {
  background-color: #f5f5f5;
  cursor: pointer;
}
</style>
