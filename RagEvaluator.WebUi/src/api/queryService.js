import axiosInstance, { apiRequest } from "./axiosConfig";

export const postQuery = (question, topK = 3, language = "en") =>
  apiRequest(() =>
    axiosInstance.post("/query", {
      Question: question,
      TopK: topK,
      Language: language,
    }),
  );

export const getAllQueries = () =>
  apiRequest(() => axiosInstance.get("/query/history"));

export const getQueryById = (id) =>
  apiRequest(() => axiosInstance.get(`/query/${id}`));

export const deleteQuery = (id) =>
  apiRequest(() => axiosInstance.delete(`/query/${id}`));

export const annotateResults = (queryId, payload) =>
  apiRequest(() => axiosInstance.patch(`/query/${queryId}/results`, payload));
