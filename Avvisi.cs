// Avvisi.cs
// Le notifiche di Windows ("Batteria scarica", "Carica completa").

using System;
using Microsoft.Win32;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace BatterieInBarra
{
    public static class Avvisi
    {
        const string ID_APP = "Simone.BatterieInBarra";

        // Windows deve conoscere il nome dell'app per mostrare le notifiche
        public static void Prepara()
        {
            try
            {
                using (RegistryKey chiave = Registry.CurrentUser.CreateSubKey(@"Software\Classes\AppUserModelId\" + ID_APP))
                {
                    chiave.SetValue("DisplayName", "Batterie in Barra");
                }
            }
            catch (Exception)
            {
                // se non riesce, le notifiche semplicemente non compaiono
            }
        }

        // Toglie il nome dell'app da Windows (quando elimini l'app)
        public static void Togli()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\AppUserModelId\" + ID_APP, false);
            }
            catch (Exception)
            {
            }
        }

        public static void Manda(string titolo, string testo)
        {
            try
            {
                // Modello di notifica con due righe: titolo e testo
                XmlDocument xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
                XmlNodeList righe = xml.GetElementsByTagName("text");
                righe[0].AppendChild(xml.CreateTextNode(titolo));
                righe[1].AppendChild(xml.CreateTextNode(testo));

                ToastNotificationManager.CreateToastNotifier(ID_APP).Show(new ToastNotification(xml));
            }
            catch (Exception)
            {
                // se le notifiche non funzionano, l'app continua lo stesso
            }
        }
    }
}
