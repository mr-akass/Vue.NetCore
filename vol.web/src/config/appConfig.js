/**
 * 多应用(子系统)配置服务
 * 应用信息存储在 Sys_Application 表，字段：
 * appId/appCode/appName/title/icon/theme/primaryColor/dataPanel/sortOrder/enabled
 * 约定：应用的 appName 与该应用的一级菜单名一致，按应用加载菜单时该一级菜单会被隐藏、其子菜单提升为一级
 */
import http from '@/api/http';

// localStorage 存储键名
export const APP_STORAGE_KEY_PREFIX = 'current_app_id_'; // 带用户标识的前缀
export const APP_LIST_KEY = 'app_list';

// 默认应用ID
export const DEFAULT_APP_ID = 1;

/** 获取当前用户ID（从 localStorage 中的用户信息获取） */
function getCurrentUserId() {
  try {
    const userStr = localStorage.getItem('user');
    if (userStr) {
      const user = JSON.parse(userStr);
      return user.userId || null;
    }
  } catch (e) {
    /* ignore parse error */
  }
  return null;
}

/** 获取带用户标识的 appId 存储 key（换账号互不污染） */
function getAppStorageKey(userId) {
  const uid = userId || getCurrentUserId();
  return uid ? `${APP_STORAGE_KEY_PREFIX}${uid}` : APP_STORAGE_KEY_PREFIX + 'anonymous';
}

/** 获取应用列表（从API获取，服务端已按当前用户角色过滤） */
export async function fetchAppList() {
  try {
    const response = await http.get('api/Sys_Application/GetEnabledApps');
    if (response && response.status && response.data) {
      return response.data;
    }
    return [];
  } catch (error) {
    console.error('Failed to fetch app list:', error);
    return [];
  }
}

export function getAppById(appId, appList) {
  if (!appList || !appList.length) return null;
  const id = parseInt(appId);
  return appList.find((app) => app.appId === id) || null;
}

export function getAppByCode(appCode, appList) {
  if (!appList || !appList.length || !appCode) return null;
  return appList.find((app) => app.appCode === appCode) || null;
}

export function getDefaultApp(appList) {
  if (!appList || !appList.length) return null;
  return getAppById(DEFAULT_APP_ID, appList) || appList[0];
}

export function isValidAppId(appId, appList) {
  return getAppById(appId, appList) !== null;
}

/** 从 localStorage 获取当前用户选择的应用ID */
export function getSavedAppId(userId) {
  const key = getAppStorageKey(userId);
  const savedId = localStorage.getItem(key);
  return savedId ? parseInt(savedId) : null;
}

/** 保存当前应用ID到 localStorage（带用户标识） */
export function saveAppId(appId, userId) {
  const key = getAppStorageKey(userId);
  localStorage.setItem(key, appId.toString());
}

/** 仅清除当前用户选择的应用ID（超管切回"完整菜单"视图用） */
export function removeSavedAppId(userId) {
  const key = getAppStorageKey(userId);
  localStorage.removeItem(key);
}

/** 清除当前用户保存的应用信息 */
export function clearAppStorage(userId) {
  const key = getAppStorageKey(userId);
  localStorage.removeItem(key);
  localStorage.removeItem(APP_LIST_KEY);
}

export default {
  APP_STORAGE_KEY_PREFIX,
  APP_LIST_KEY,
  DEFAULT_APP_ID,
  fetchAppList,
  getAppById,
  getAppByCode,
  getDefaultApp,
  isValidAppId,
  getSavedAppId,
  saveAppId,
  clearAppStorage
};
