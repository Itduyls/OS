import { createStore } from 'vuex'
export const store = createStore({
    state: {
        islogin: false,
        isadmin: false,
        background: localStorage.getItem("background") || "https://primefaces.org/primevue/img/window.6b46439b.jpg",
        isDock: localStorage.getItem("isDock") == "true",
        user: {},
        token: null,
        urlLang: "/Portals/Lang/VietNam.png",
        langId: 1,
        isLoadLang: false,
        idNews: null,
    },
    getters: {
        langid: state => state.langId,
        islogin: state => state.islogin,
        user: state => state.user,
        token: state => state.token,
        isDock: state => state.isDock,
        background: state => state.background,
        isadmin: state => state.isadmin
    },
    mutations: {
        setisadmin(state, vl) {
            state.isadmin = vl;
        },
        setIdNews(state, v1) {
            state.idNews = v1;
        },
        setLangId(state, v1) {
            state.langId = v1;
        },
        setIsLoadLang(state, v1) {
            state.isLoadLang = v1;
        },
        setUrlLang(state, v1) {
            state.urlLang = v1;
        },
        setislogin(state, vl) {
            state.islogin = vl;
        },
        setisDock(state, vl) {
            state.isDock = vl;
            localStorage.setItem("isDock", vl);
        },
        setbackground(state, vl) {
            state.background = vl;
            localStorage.setItem("background", vl);
        },
        setuser(state, vl) {
            state.user = vl;
        },
        settoken(state, vl) {
            state.token = vl;
        },
        gologout(state, router) {
            localStorage.removeItem("tk");
            state.token = null;
            state.islogin = false;
            state.user = null;
            if (router)
                router.push({ path: "/" });
        }
    }
});