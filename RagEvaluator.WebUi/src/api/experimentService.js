import axiosInstance, { apiRequest } from './axiosConfig';

export const createExperiment = (experimentData) =>
  apiRequest(() => axiosInstance.post('/experiments', experimentData));
