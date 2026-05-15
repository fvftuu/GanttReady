// ============================================================
// storage/project.ts — localStorage 项目选择
// ============================================================
const ACTIVE_PROJECT_KEY = 'netplan_active_project';
const CHECKED_PROJECT_KEY = 'netplan_checked_project';
/** 获取当前活跃项目 ID */
export function getActiveProject() {
    try {
        return localStorage.getItem(ACTIVE_PROJECT_KEY);
    }
    catch {
        return null;
    }
}
/** 设置当前活跃项目 ID */
export function setActiveProject(id) {
    try {
        localStorage.setItem(ACTIVE_PROJECT_KEY, id);
    }
    catch {
        // 忽略
    }
}
/** 导航到项目页面 */
export function navToProject(page, id) {
    setActiveProject(id);
    window.location.href = `/project/${page}?id=${id}`;
}
/** 获取首页勾选的项目 ID */
export function getCheckedProject() {
    try {
        return localStorage.getItem(CHECKED_PROJECT_KEY);
    }
    catch {
        return null;
    }
}
/** 设置首页勾选的项目 ID */
export function setCheckedProject(id) {
    try {
        localStorage.setItem(CHECKED_PROJECT_KEY, id);
    }
    catch {
        // 忽略
    }
}
/** 导航到勾选项目的页面 */
export function navToChecked(page) {
    const id = getCheckedProject();
    if (id) {
        window.location.href = `/project/${page}?id=${id}`;
    }
}
