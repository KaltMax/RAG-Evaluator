import axiosInstance, { apiRequest } from './axiosConfig';

export const getSettings = () =>
  apiRequest(() => axiosInstance.get('/settings'));

export const updateSettings = (settings) =>
  apiRequest(() => axiosInstance.patch('/settings', settings));
