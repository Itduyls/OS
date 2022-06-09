<script setup>
//Import
import { RouterView } from "vue-router";
import HeadBar from "./components/base/HeadBar.vue";
import HeadBarPublic from "./publicview/Heabar.vue";
import SideBar from "./components/base/SideBar.vue";
import { inject, onMounted, ref } from "vue";
import { decr } from "./util/function";
import { useToast } from "vue-toastification";

//Khai báo biến
const toast = useToast();
const cryoptojs = inject("cryptojs");
const store = inject("store");
//init Data
if (localStorage.getItem("u") != null) {
  let u = decr(localStorage.getItem("u"), SecretKey, cryoptojs);
  if (u != null) {
    store.commit("setuser", JSON.parse(u));
  }
}
//Vue App
onMounted(() => {
  return {};
});
</script>

<template>
<div class="w-full ">
 <SideBar
   
    class="shadow-1 h-full"
    v-if="store.getters.islogin"
  />
  <div class="flex flex-column flex-grow-1 h-full" v-if="!store.getters.isDock">
    <HeadBar v-if="store.getters.islogin" />
    <RouterView />
  </div>
</div>
 
</template>
<style>
.ck-editor__editable {
  min-height: 150px !important;
}
</style>
