<script setup>
//Import
import { inject, onMounted, ref } from "vue";
//Khai báo biến
const basedomainURL = baseURL;
const store = inject("store");
const swal = inject("$swal");
const axios = inject("axios"); // inject axios
const menu = ref([
  {
    header: "CMS V5",
    hiddenOnCollapse: true,
  },
  {
    href: "/",
    title: "Dashboard",
    icon: "pi pi-home",
  }
]);
const config = {
  headers: { Authorization: `Bearer ${store.getters.token}` },
};
const appconfig = ref({ version: "1.0" });
const initModule = () => {
  axios
    .get(baseURL + "/api/Cache/ListModuleUserCache?cache=" + store.getters.user.Users_ID, {
      headers: { Authorization: `Bearer ${store.getters.token}` },
    })
    .then((response) => {
      console.log("vao");
      let data = JSON.parse(response.data.data)[0];
      
      if (data.length == 0) {
        //Chưa có Module
        let obj = {
          title: "Hệ thống",
          icon: "pi pi-cog",
          child: [],
        };
        obj.child.push({
          href: "/module",
          title: "Menu chức năng",
        });
        menu.value.push(obj);
      } else {
        
        data
          .filter((x) => x.parent_id == null)
          .forEach((md) => {
            let obj = {
              title: md.module_name,
              icon: md.icon,
              href: md.is_link,
            };
            let childs = data.filter((x) => x.parent_id == md.module_id);
            if (childs.length > 0) {
              obj.child = [];
              childs.forEach((md1) => {
                let obj1 = {
                  title: md1.module_name,
                  icon: md1.icon,
                  href: md1.is_link,
                };
                obj.child.push(obj1);
              });
            }
            menu.value.push(obj);
          });
      }
    })
    .catch((error) => {
     console.log("ree",error,store.getters.token);
      if (error && error.status === 401) {
        
        swal.fire({
          title: "Error!",
          text: "Mã token đã hết hạn hoặc không hợp lệ, vui lòng đăng nhập lại!",
          icon: "error",
          confirmButtonText: "OK",
        });
      //  store.commit("gologout");
      }
    });
};
//Vue App
onMounted(() => {
  initModule();
  return {
    menu,
  };
});
</script>
<template>
  <sidebar-menu :menu="menu" class="vsm_white-theme " >
    <!-- <template v-slot:header
      ><div class="text-center p-2">
        <Avatar
          v-ripple
          v-bind:image="basedomainURL + store.getters.user.Avartar"
          v-bind:label="store.getters.user.Avartar ? '' : store.getters.user.FName"
          size="large"
          style="background-color: #2196f3; color: #ffffff"
          shape="circle"
        />
      </div>
    </template> -->
    <template v-slot:footer></template>
    <template v-slot:toggle-icon>
      <i class="pi pi-microsoft" style="font-size: 2rem"></i>
      <h5 class="ml-2 hversion">Phiên bản {{ appconfig.version }}</h5>
    </template>
  </sidebar-menu>
</template>
<style scoped>
.AppSideBar {
  background-color: #fff;
}
.vsm_white-theme {
  font-weight: 700;
  height: 100vh !important;
}
.vsm_collapsed .hversion {
  display: none;
}

</style>
