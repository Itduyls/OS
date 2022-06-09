import { createRouter, createWebHistory } from "vue-router";
const HomeView = () =>
    import ('../views/HomeView.vue')
const ModulesView = () =>
    import ('../views/hethong/ModulesView.vue')
const SQLTableView = () =>
    import ('../views/hethong/SQLTableView.vue')
const UserView = () =>
    import ('../views/hethong/UserView.vue')
const RolesView = () =>
    import ('../views/hethong/RolesView.vue')
const WebAcessView = () =>
    import ('../views/hethong/WebAcessView.vue')
const LogsView = () =>
    import ('../views/hethong/LogsView.vue')
const TestCaseView = () =>
    import ('../views/hethong/TestCaseView.vue')
const ConfigView = () =>
    import ('../views/hethong/ConfigView.vue')
const SQLView = () =>
    import ('../views/hethong/SQLView.vue')
const DonviView = () =>
    import ('../views/hethong/DonviView.vue')
const LoginView = () =>
    import ('../views/LoginView.vue')
const Places = () =>
    import ('../views/dictionary/Places.vue')
const Positions = () =>
    import ('../views/dictionary/Positions.vue')
const Dispatch = () =>
    import ('../views/dictionary/Dispatch.vue')
const Email = () =>
    import ('../views/dictionary/Email.vue')
const Cagroup = () =>
    import ('../views/dictionary/Cagroup.vue')
const Field = () =>
    import ('../views/dictionary/Field.vue')
const IssuePlace = () =>
    import ('../views/dictionary/IssuePlace.vue')
const RecevePlace = () =>
    import ('../views/dictionary/RecevePlace.vue')
const Security = () =>
    import ('../views/dictionary/Security.vue')
const SendWay = () =>
    import ('../views/dictionary/SendWay.vue')
const Signer = () =>
    import ('../views/dictionary/Signer.vue')
const Urgency = () =>
    import ('../views/dictionary/Urgency.vue')
const CaPosition = () =>
    import ('../views/dictionary/CaPosition.vue')
const Tem = () =>
    import ('../views/dictionary/Tem.vue')
const Tags = () =>
    import ('../views/dictionary/Tags.vue')
const Type = () =>
    import ('../views/dictionary/Type.vue')
const Status = () =>
    import ('../views/dictionary/Status.vue')
const Project = () =>
    import ('../views/project/Project.vue')
const Api = () =>
    import ('../views/project/Api.vue')
const Table = () =>
    import ('../views/project/Table.vue')
const Plugin = () =>
    import ('../views/project/Plugin.vue')
const router = createRouter({
    history: createWebHistory(
        import.meta.env.BASE_URL),
    routes: [{
            path: "/",
            name: "home",
            component: HomeView,
        },
        {
            path: "/place",
            name: "place",
            component: Places,
        },
        {
            path: "/position",
            name: "position",
            component: Positions,
        },
        //Từ điển
        {
            path: "/dispatch",
            name: "dispatch",
            component: Dispatch,
        },
        {
            path: "/email",
            name: "email",
            component: Email,
        },
        {
            path: "/cagroup",
            name: "cagroup",
            component: Cagroup,
        },
        {
            path: "/field",
            name: "field",
            component: Field,
        },
        {
            path: "/issueplace",
            name: "issueplace",
            component: IssuePlace,
        },
        {
            path: "/receveplace",
            name: "receveplace",
            component: RecevePlace,
        },
        {
            path: "/security",
            name: "security",
            component: Security,
        },
        {
            path: "/sendway",
            name: "sendway",
            component: SendWay,
        },
        {
            path: "/signer",
            name: "signer",
            component: Signer,
        },
        {
            path: "/urgency",
            name: "urgency",
            component: Urgency,
        },
        {
            path: "/caposition",
            name: "caposition",
            component: CaPosition,
        },
        {
            path: "/tem",
            name: "tem",
            component: Tem,
        },
        {
            path: "/type",
            name: "type",
            component: Type,
        },
        {
            path: "/tags",
            name: "tags",
            component: Tags,
        },
        {
            path: "/status",
            name: "status",
            component: Status,
        },
        {
            path: "/project",
            name: "project",
            component: Project,
        },
        {
            path: "/api",
            name: "api",
            component: Api,
        },
        {
            path: "/table",
            name: "table",
            component: Table,
        },
        {
            path: "/plugin",
            name: "plugin",
            component: Plugin,
        },

        //Hệ thống
        {
            path: "/module",
            name: "module",
            component: ModulesView,
        },
        {
            path: "/user",
            name: "user",
            component: UserView,
        },
        {
            path: "/tables",
            name: "tables",
            component: SQLTableView,
        },
        {
            path: "/history",
            name: "history",
            component: WebAcessView,
        },
        {
            path: "/logs",
            name: "logs",
            component: LogsView,
        },
        {
            path: "/testcase",
            name: "testcase",
            component: TestCaseView,
        },
        {
            path: "/config",
            name: "config",
            component: ConfigView,
        },
        {
            path: "/sql",
            name: "sql",
            component: SQLView,
        },
        {
            path: "/role",
            name: "role",
            component: RolesView,
        },
        {
            path: "/login",
            name: "login",
            component: LoginView,
        },
        {
            path: "/donvi",
            name: "donvi",
            component: DonviView,
        },
    ],
});

export default router;