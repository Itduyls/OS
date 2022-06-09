<script setup>
import LoginView from "./LoginView.vue";
import { inject, onMounted } from "vue";
const store = inject("store");
onMounted(() => {

  setTimeout(store.commit("setisadmin",true), 3000) 
  return { 
  };
});
</script>
<template>
  <LoginView v-if="!store.getters.islogin" />
  <div
    class="main-layout flex-grow-1"
    v-if="store.getters.islogin"
   
  ></div>
</template>
<style scoped>
.main-layout {
  background-color: #eee;
}
</style>
