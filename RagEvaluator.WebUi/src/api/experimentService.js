import axiosInstance, { apiRequest } from "./axiosConfig";

export const createExperiment = (experimentData) =>
  apiRequest(() => axiosInstance.post("/experiments", experimentData));

export const getExperiments = () =>
  apiRequest(() => axiosInstance.get("/experiments"));

export const getExperimentById = (id) =>
  apiRequest(() => axiosInstance.get(`/experiments/${id}`));

export const deleteExperiment = (id) =>
  apiRequest(() => axiosInstance.delete(`/experiments/${id}`));
