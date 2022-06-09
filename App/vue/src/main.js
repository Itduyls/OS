import { createApp, ref } from "vue";
import App from "./App.vue"
import router from "./router";
import VueSweetalert2 from 'vue-sweetalert2';
import VueCryptojs from 'vue-cryptojs';
import PrimeVue from 'primevue/config';
import Button from 'primevue/button'
import AutoComplete from 'primevue/autocomplete';
import Accordion from 'primevue/accordion';
import AccordionTab from 'primevue/accordiontab';
import Avatar from 'primevue/avatar';
import AvatarGroup from 'primevue/avatargroup';
import Badge from 'primevue/badge';
import BadgeDirective from "primevue/badgedirective";
import BlockUI from 'primevue/blockui';
import Breadcrumb from 'primevue/breadcrumb';
import Calendar from 'primevue/calendar';
import Card from 'primevue/card';
import CascadeSelect from 'primevue/cascadeselect';
import Carousel from 'primevue/carousel';
import Checkbox from 'primevue/checkbox';
import Chip from 'primevue/chip';
import Chips from 'primevue/chips';
// import ColorPicker from 'primevue/colorpicker';
import Column from 'primevue/column';
import ColumnGroup from 'primevue/columngroup';
import ConfirmDialog from 'primevue/confirmdialog';
import ConfirmPopup from 'primevue/confirmpopup';
import ConfirmationService from 'primevue/confirmationservice';
import ContextMenu from 'primevue/contextmenu';
import DataTable from 'primevue/datatable';
import DataView from 'primevue/dataview';
import DataViewLayoutOptions from 'primevue/dataviewlayoutoptions';
import DeferredContent from 'primevue/deferredcontent';
import Dialog from 'primevue/dialog';
import Divider from 'primevue/divider';
import Dock from 'primevue/dock';
import Dropdown from 'primevue/dropdown';
import Fieldset from 'primevue/fieldset';
import FileUpload from 'primevue/fileupload';
import Galleria from 'primevue/galleria';
import Image from 'primevue/image';
import InlineMessage from 'primevue/inlinemessage';
import Inplace from 'primevue/inplace';
import InputSwitch from 'primevue/inputswitch';
import InputText from 'primevue/inputtext';
import InputMask from 'primevue/inputmask';
import InputNumber from 'primevue/inputnumber';
import Knob from 'primevue/knob';
import Listbox from 'primevue/listbox';
import MegaMenu from 'primevue/megamenu';
import Menu from 'primevue/menu';
import Menubar from 'primevue/menubar';
import Message from 'primevue/message';
import MultiSelect from 'primevue/multiselect';
import OrderList from 'primevue/orderlist';
import OrganizationChart from 'primevue/organizationchart';
import OverlayPanel from 'primevue/overlaypanel';
import Paginator from 'primevue/paginator';
import Panel from 'primevue/panel';
import PanelMenu from 'primevue/panelmenu';
import Password from 'primevue/password';
import PickList from 'primevue/picklist';
import ProgressBar from 'primevue/progressbar';
import ProgressSpinner from 'primevue/progressspinner';
import Rating from 'primevue/rating';
import RadioButton from 'primevue/radiobutton';
import Ripple from 'primevue/ripple';
import Row from 'primevue/row';
import SelectButton from 'primevue/selectbutton';
import ScrollPanel from 'primevue/scrollpanel';
import ScrollTop from 'primevue/scrolltop';
import Skeleton from 'primevue/skeleton';
import Slider from 'primevue/slider';
import Sidebar from 'primevue/sidebar';
import SpeedDial from 'primevue/speeddial';
import SplitButton from 'primevue/splitbutton';
import Splitter from 'primevue/splitter';
import SplitterPanel from 'primevue/splitterpanel';
import TabMenu from 'primevue/tabmenu';
import TieredMenu from 'primevue/tieredmenu';
import Textarea from 'primevue/textarea';
//import Toast from 'primevue/toast';
import ToastService from 'primevue/toastservice';
import Toolbar from 'primevue/toolbar';
import TabView from 'primevue/tabview';
import TabPanel from 'primevue/tabpanel';
import Tag from 'primevue/tag';
import Terminal from 'primevue/terminal';
import Timeline from 'primevue/timeline';
import ToggleButton from 'primevue/togglebutton';
import Tooltip from 'primevue/tooltip';
import Tree from 'primevue/tree';
import TreeSelect from 'primevue/treeselect';
import TreeTable from 'primevue/treetable';
import TriStateCheckbox from 'primevue/tristatecheckbox';
import VirtualScroller from 'primevue/virtualscroller';
import Steps from 'primevue/steps';
import VueSidebarMenu from 'vue-sidebar-menu'
import Toast from "vue-toastification";
import Editor from 'primevue/editor';
import axios from 'redaxios'
import VueAxios from 'vue-axios'
import VueAnimateOnScroll from 'vue-animate-onscroll'
// Import the CSS or use your own!
import "vue-toastification/dist/index.css";
import 'sweetalert2/dist/sweetalert2.min.css';
import 'vue-sidebar-menu/dist/vue-sidebar-menu.css'
import 'primevue/resources/themes/saga-blue/theme.css' //theme
import 'primevue/resources/primevue.min.css' //core css
import 'primeicons/primeicons.css'
import 'primeflex/primeflex.css'
import 'vue-color-kit/dist/vue-color-kit.css'

import { computed, watchEffect } from "@vue/runtime-core";
import moment from "moment";
import mitt from 'mitt'
import VueSocketIOExt from 'vue-socket.io-extended';
import { io } from 'socket.io-client';
import { store } from './store/store.js';
import { ColorPicker } from 'vue-color-kit'
import CKEditor from '@ckeditor/ckeditor5-vue';
Date.prototype.toISOString = function() {
    return moment(this).format("YYYY-MM-DDTHH:mm:ss");
};

let storetheme = ref("saga-blue");
if (localStorage.getItem("storetheme")) {
    storetheme.value = localStorage.getItem("storetheme");
}
let first = true;
const theme = computed(() => storetheme.value);
let kversion = 0;
watchEffect(() => {
    if (!first) {
        let themcss = document.getElementsByTagName("style");
        if (themcss.length > 0) {
            themcss[themcss.length - 1].remove();
        }
    }
    first = false;
    kversion++;
    import (`./assets/css/themes/${theme.value}/theme.css?v=${kversion}`);
});
//End CSS
const app = createApp(App);
app.use(VueAnimateOnScroll);
app.use(router);
app.use(VueAxios, axios);
app.use(store);
const options = {
    confirmButtonColor: '#0d6efd',
    cancelButtonColor: '#ff7674',
};
axios.defaults.baseURL = baseURL;
const emitter = mitt()
app.config.globalProperties.emitter = emitter;
app.provide('emitter', emitter);
// Assign the global variable before mounting
let tk = localStorage.getItem("tk");
store.commit("setislogin", tk != null);
store.commit("settoken", tk);
app.provide('storetheme', storetheme);
app.provide('router', router);
app.provide('axios', app.config.globalProperties.axios) // provide 'axios'
app.use(VueSweetalert2, options);
app.use(VueCryptojs);
app.use(VueSidebarMenu);

app.use(PrimeVue, {
    locale: {
        startsWith: 'Bắt đầu với',
        contains: 'Gồm',
        notContains: 'Không gồm',
        endsWith: 'Kết thúc với',
        equals: 'Bằng',
        notEquals: 'Không bằng',
        noFilter: 'Không lọc',
        lt: 'Ít hơn',
        lte: 'Ít hơn hoặc bằng',
        gt: 'Nhiều hơn',
        gte: 'Nhiều hơn hoặc bằng',
        dateIs: 'Ngày bằng',
        dateIsNot: 'Khác ngày',
        dateBefore: 'Trước ngày',
        dateAfter: 'Sau ngày',
        clear: 'Xoá',
        apply: 'Thực hiện',
        matchAll: 'Tìm kết hợp',
        matchAny: 'Tìm 1 hoặc nhiều tiêu chí',
        addRule: 'Thêm điều kiện',
        removeRule: 'Xoá điều kiện',
        accept: 'Yes',
        reject: 'No',
        choose: 'Choose',
        upload: 'Upload',
        cancel: 'Cancel',
        dayNames: ["Chủ nhật", "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7"],
        dayNamesShort: ["CN", "T2", "T3", "T4", "T5", "T6", "T7"],
        dayNamesMin: ["CN", "T2", "T3", "T4", "T5", "T6", "T7"],
        monthNames: ["Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6", "Tháng 7", "Tháng 8", "Tháng 9", "Tháng 10", "Tháng 11", "Tháng 12"],
        monthNamesShort: ["T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12"],
        today: 'Hôm nay',
        weekHeader: 'Tuần',
        firstDayOfWeek: 0,
        dateFormat: 'dd/mm/yy',
        weak: 'Yếu',
        medium: 'Bình thường',
        strong: 'Mạnh',
        passwordPrompt: 'Nhâoj mật khẩu',
        emptyFilterMessage: 'Không có bản ghi nào',
        emptyMessage: 'Không có tùy chọn nào'
    }

});
app.use(Toast);
app.use(ConfirmationService);
app.use(ToastService);
app.component('Editor', Editor);
app.component('Steps', Steps);
app.component('Button', Button);
app.component('Accordion', Accordion);
app.component('AccordionTab', AccordionTab);
app.component('AutoComplete', AutoComplete);
app.component('Avatar', Avatar);
app.component('AvatarGroup', AvatarGroup);
app.component('Badge', Badge);
app.component('BlockUI', BlockUI);
app.component('Breadcrumb', Breadcrumb);
app.component('Calendar', Calendar);
app.component('Card', Card);
app.component('Carousel', Carousel);
app.component('CascadeSelect', CascadeSelect);
app.component('Checkbox', Checkbox);
app.component('Chip', Chip);
app.component('Chips', Chips);
// app.component('ColorPicker', ColorPicker);
app.component('Column', Column);
app.component('ColumnGroup', ColumnGroup);
app.component('ConfirmDialog', ConfirmDialog);
app.component('ConfirmPopup', ConfirmPopup);
app.component('ContextMenu', ContextMenu);
app.component('DataTable', DataTable);
app.component('DataView', DataView);
app.component('DataViewLayoutOptions', DataViewLayoutOptions);
app.component('DeferredContent', DeferredContent);
app.component('Dialog', Dialog);
app.component('Divider', Divider);
app.component('Dock', Dock);
app.component('Dropdown', Dropdown);
app.component('Fieldset', Fieldset);
app.component('FileUpload', FileUpload);
app.component('Galleria', Galleria);
app.component('Image', Image);
app.component('InlineMessage', InlineMessage);
app.component('Inplace', Inplace);
app.component('InputMask', InputMask);
app.component('InputNumber', InputNumber);
app.component('InputSwitch', InputSwitch);
app.component('InputText', InputText);
app.component('Knob', Knob);
app.component('Listbox', Listbox);
app.component('MegaMenu', MegaMenu);
app.component('Menu', Menu);
app.component('Menubar', Menubar);
app.component('Message', Message);
app.component('MultiSelect', MultiSelect);
app.component('OrderList', OrderList);
app.component('OrganizationChart', OrganizationChart);
app.component('OverlayPanel', OverlayPanel);
app.component('Paginator', Paginator);
app.component('Panel', Panel);
app.component('PanelMenu', PanelMenu);
app.component('Password', Password);
app.component('PickList', PickList);
app.component('ProgressBar', ProgressBar);
app.component('ProgressSpinner', ProgressSpinner);
app.component('RadioButton', RadioButton);
app.component('Rating', Rating);
app.component('Row', Row);
app.component('SelectButton', SelectButton);
app.component('ScrollPanel', ScrollPanel);
app.component('ScrollTop', ScrollTop);
app.component('Slider', Slider);
app.component('Sidebar', Sidebar);
app.component('Skeleton', Skeleton);
app.component('SpeedDial', SpeedDial);
app.component('SplitButton', SplitButton);
app.component('Splitter', Splitter);
app.component('SplitterPanel', SplitterPanel);
app.component('TabMenu', TabMenu);
app.component('TabView', TabView);
app.component('TabPanel', TabPanel);
app.component('Tag', Tag);
app.component('Textarea', Textarea);
app.component('Terminal', Terminal);
app.component('TieredMenu', TieredMenu);
app.component('Timeline', Timeline);
//app.component('Toast', Toast);
app.component('Toolbar', Toolbar);
app.component('ToggleButton', ToggleButton);
app.component('Tree', Tree);
app.component('TreeSelect', TreeSelect);
app.component('TreeTable', TreeTable);
app.component('TriStateCheckbox', TriStateCheckbox);
app.component('VirtualScroller', VirtualScroller);
app.component('Tooltip', Tooltip);
app.component('ColorPicker', ColorPicker);
app.directive('badge', BadgeDirective);
app.directive('ripple', Ripple);
app.directive("tooltip", Tooltip);
app.use(CKEditor).mount("#app");