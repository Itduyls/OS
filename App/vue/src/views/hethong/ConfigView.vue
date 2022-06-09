<script setup>
import { inject, onMounted, ref } from "vue";
import { useToast } from "vue-toastification";
const axios = inject("axios"); // inject axios
const config = ref({});
const store = inject("store");
const swal = inject("$swal");
const toast = useToast();

const initConfig = () => {
  axios
    .get(baseURL + "/api/CAche/GetConfig", {
      headers: { Authorization: `Bearer ${store.getters.token}` },
    })
    .then((response) => {
      swal.close();
      if (response.data.err != "1") {
        config.value = response.data.data;
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

const saveConfig = () => {
  swal.fire({
    width: 110,
    didOpen: () => {
      swal.showLoading();
    },
  });
  axios
    .post(baseURL + "/api/CAche/SetConfig", config.value, {
      headers: { Authorization: `Bearer ${store.getters.token}` },
    })
    .then((response) => {
      swal.close();
      if (response.data.err != "1") {
        toast.success("Cập nhật thiết lập thành công!");
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
onMounted(() => {
  store.commit("setisadmin",true);
  initConfig();
  return {
    saveConfig,
  };
});
</script>
<template>
  <div class="main-layout h-full p-4" v-if="store.getters.islogin">
    <Card class="p-4">
      <template #header>
        <h3><i class="pi pi-cog"></i> Thiết lập tham số hệ thống</h3>
      </template>

      <template #content>
        <form @submit.prevent="saveConfig">
          <div class="grid formgrid m-2">
            <div class="field col-12 md:col-12">
              <label class="col-4 text-left" style="vertical-align: text-bottom"
                >Bật Debug
              </label>
              <InputSwitch v-model="config.debug" class="col-8" />
            </div>
            <div class="field col-12 md:col-12">
              <label class="col-4 text-left" style="vertical-align: text-bottom"
                >Bật Lưu Log
              </label>
              <InputSwitch v-model="config.wlog" class="col-8" />
            </div>
            <div class="field col-12 md:col-12">
              <label class="col-4 text-left">Số phút lưu trạng thái Token</label>
              <InputNumber class="col-8 ip36 p-0" v-model="config.timeout" />
            </div>
            <div class="field col-12 md:col-12">
              <label class="col-4 text-left"
                >Thời gian tối thiểu để lưu Log khi chạy SQL (Milisec)
              </label>
              <InputNumber class="col-8 ip36 p-0" v-model="config.milisec" />
            </div>
            <div class="field col-12 md:col-12">
              <label class="col-4 text-left">Thông báo lỗi </label>
              <InputText
                spellcheck="false"
                class="col-8 ip36"
                v-model="config.logCongtent"
              />
            </div>
            <div class="field col-12 md:col-12">
              <label class="col-4 text-left">Version Cache </label>
              <InputNumber class="col-8 ip36 p-0" v-model="config.cache" />
            </div>
          </div>
        </form>
      </template>
      <template #footer>
        <div class="text-center">
          <Button icon="pi pi-save" label="Cập nhật" @click="saveConfig" />
        </div>
      </template>
    </Card>
  </div>
</template>
