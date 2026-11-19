using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using TYPSA.SharedLib.Autocad;
using TYPSA.SharedLib.EndPoints;
using TYPSA.SharedLib.UserForms;
using TYPSA.SharedLib.ExcelAutocad;
using TYPSA.SharedLib.Metrics;
using static TYPSA.SharedLib.Autocad.cls_00_CadInfoHelper;

namespace TNG.RibbonButton.Autocad
{
    public class cls_00_MainTngModelChecker
    {
        public ProcessResult MainTngModelChecker(
            string selectedFolderPath,
            string[] selectedFiles,
            List<string> selectedOptions,
            AteneaSessionInfo sessionInfo,
            UiTexts uiTexts,
            bool isSpanish
            
        )
        {
            // ---------------------------------
            // Obtener informacion sesion
            // ---------------------------------

            CadSessionInfo infoCad = GetCivilSessionInfo(sessionInfo);

            // -------------------------------
            // Definir variables
            // -------------------------------

            List<Dictionary<string, object>> dataJsonByModel = new List<Dictionary<string, object>>();
            List<WarningCheckLogResult> warningChecksLog = new List<WarningCheckLogResult>();
            List<List<string>> dataToExcelGlobal = new List<List<string>>();
            ModelCheckerResults resultsfromcad = new ModelCheckerResults();
            ModelCheckerKeys keys = new ModelCheckerKeys(isSpanish);
            string msg = string.Empty;

            // -------------------------------
            // Tiempos de ejecución
            // -------------------------------

            // Global
            Dictionary<string, TimeSpan> processDurations = new Dictionary<string, TimeSpan>();
            // Modelo
            Dictionary<string, Dictionary<string, TimeSpan>> processDurationsByModel =
                new Dictionary<string, Dictionary<string, TimeSpan>>();

            Stopwatch totalStopwatch = Stopwatch.StartNew();

            // -------------------------------
            // Normalizar idioma
            // -------------------------------

            string softwareLanguage = isSpanish ? "Spanish" : "English";

            // -------------------------------
            // Reiniciamos cronometro global
            // -------------------------------

            sessionInfo.StartTime = DateTime.Now;

            // -------------------------------
            // Crear progreso
            // -------------------------------

            ProgressBarControl progressBarForm = new ProgressBarControl();
            // Iniciamos variables
            int totalFiles = selectedFiles.Length;
            int processedFiles = 0;

            // Mostramos
            progressBarForm.ProgressValue = 10; // Valor inicial 
            progressBarForm.Show();

            // try
            try
            {
                // -----------------------------
                // Procesar modelos seleccionados
                // -----------------------------

                cls_00_ProcessFilesModelChecker.ProcessSelectedModels(
                    selectedFiles, selectedOptions, keys, isSpanish, warningChecksLog, resultsfromcad, 
                    dataJsonByModel, processDurations, processDurationsByModel, progressBarForm, ref processedFiles
                );

                // -----------------------------
                // Cerrar progreso
                // -----------------------------

                progressBarForm.Close();

                // -----------------------------
                // Construir JSON Global
                // -----------------------------

                Dictionary<string, object> dictDataByFileToJson = cls_00_AteneaSessionInfo.GetFinalJsonDictionaryTng(
                    cls_00_AteneaJson.User, cls_00_AteneaJson.CivilVersion, cls_00_AteneaJson.CivilLanguage, 
                    dataJsonByModel, sessionInfo
                );

                // -----------------------------
                // Validar y guardar JSON
                // -----------------------------

                bool jsonSuccess = cls_00_PrepareDataModelCheck.ValidateAndSaveModelData(
                    sessionInfo, infoCad.RootFolderNameTng, infoCad.JsonFileNameDataExtractionTng,
                    dictDataByFileToJson, selectedFolderPath, dataJsonByModel, processDurations, isSpanish
                );
                // Validamos
                if (!jsonSuccess) return new ProcessResult();

                // -----------------------------
                // Preparar datos para informes
                // -----------------------------

                Dictionary<string, object> exportData = cls_00_GetReportData.GetReportData(
                    keys, selectedOptions, resultsfromcad, warningChecksLog, processDurations, isSpanish
                );
                // Validamos
                if (exportData == null) return new ProcessResult();

                // -----------------------------
                // Generar informes finales
                // -----------------------------

                Stopwatch processStopwatch = Stopwatch.StartNew();

                msg = cls_00_ProcessMessages.ShowProcessMessage(
                    isSpanish, cls_00_ProcessMessages.GenerateFinalReports
                );

                // Excel
                cls_00_ExportModCheckToExcel_OpenXml.ExportDataToExcel(exportData);

                // Html
                cls_00_ExportTgnCheckToHtml.ExportToHtml(
                    selectedFolderPath, exportData, warningChecksLog, sessionInfo.ProjectCode, totalFiles, processedFiles
                );

                cls_00_ProcessMessages.AddProcessDuration(
                    processDurations, msg, processStopwatch
                );

                // -------------------------------
                // Tiempo total
                // -------------------------------

                totalStopwatch.Stop();

                processDurations["Total"] = totalStopwatch.Elapsed;

                // -------------------------------
                // Resumen
                // -------------------------------

                DateTime endTime = DateTime.Now;
                TimeSpan duration = endTime - sessionInfo.StartTime;

                // Mensaje
                MessageBox.Show(
                    uiTexts.MsgCompleted +
                    "\nDuration: " + duration.ToString(@"hh\:mm\:ss") +
                    "\nStarted at: " + sessionInfo.StartTime.ToString("HH:mm:ss") +
                    "\nEnded at: " + endTime.ToString("HH:mm:ss"),
                    uiTexts.Title, MessageBoxButtons.OK, MessageBoxIcon.Information
                );

                // Enviar Metricas Serapis
                cls_00_SerapisMetrics.InitializeMetricsAsync(SerapisMetricIds.TngAutocad);

                // return
                return new ProcessResult
                {
                    TotalFilesProcessed = processedFiles,
                    ParametersAnalyzed = processedFiles
                };
            }
            // catch
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // Por defecto
            return new ProcessResult();
        }
        

    }
}
