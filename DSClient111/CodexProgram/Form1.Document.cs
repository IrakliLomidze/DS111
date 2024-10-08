using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Windows.Forms;
using TXTextControl;
using ILG.DS.Controls;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using ILG.DS.Models.History;
using ILG.DS.Zip;
using ILG.DS.AppStateManagement;
using ILG.DS;
using ILG.DS.Configurations;
using ILG.DS.Dialogs;
using ILG.DS.MSWord;
using System.Diagnostics;

namespace ILG.Codex.CodexR4
{
    public partial class Form1
    {
        public string DS_DocEncoding;
  
        public string DS_DocumentTitle;

        public int DS_DocumentFormat;
        public string DS_Comments;
        public string DS_Additional;

        public string ZipFileName;
        public bool HasAttachmen;

        private bool HistoryMode;


        #region ICON

        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        class Win32
        {
            public const uint SHGFI_ICON = 0x100;
            public const uint SHGFI_LARGEICON = 0x0; // 'Large icon
            public const uint SHGFI_SMALLICON = 0x1; // 'Small icon

            //[DllImport("shell32.dll")]
            //public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);
            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr SHGetFileInfo([MarshalAs(UnmanagedType.LPWStr)]string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);
        }

        #endregion ICON


        public bool HasDocumentHistory(int ID)
        {
            string strsql = $"Select Count(C_ID) from CodexDS_DDOCS_History WHERE (C_ID = {ID.ToString()})  AND  (H_Status = 0)  ";
            if (app.state.RunTimeLicense.IsHistoryExtendedAccessGranted() == true) strsql = $"Select Count(C_ID) from CodexDS_DDOCS_History WHERE (C_ID = {ID.ToString()}) AND ((H_STATUS = 0) OR (H_STATUS = 1)) " ;
            if (app.state.RunTimeLicense.IsRecoverDeletedDocumentsGranted() == true) strsql = $"Select Count(C_ID) from CodexDS_DDOCS_History WHERE (C_ID = {ID.ToString()}) ";

            bool result = false;
            using (var cn = new SqlConnection(app.state.ConnectionString))
            {
                cn.Open();
                SqlCommand cm = new SqlCommand(strsql, cn);
                result = ((int)cm.ExecuteScalar() > 0);
            }
            return result;
        }

        HistoryMenuModel history_data_model = new HistoryMenuModel();
        public string HistoryListDateTimetoGString(DateTime dt)
        {
            String Str = "";
            switch (dt.Month)
            {
                case 1: Str = "იანვარი"; break;
                case 2: Str = "თებერვალი"; break;
                case 3: Str = "მარტი"; break;
                case 4: Str = "აპრილი"; break;
                case 5: Str = "მაისი"; break;
                case 6: Str = "ივნისი"; break;
                case 7: Str = "ივლისი"; break;
                case 8: Str = "აგვისტო"; break;
                case 9: Str = "სექტემბერი"; break;
                case 10: Str = "ოქტომბერი"; break;
                case 11: Str = "ნოემბერი"; break;
                case 12: Str = "დეკემბერი"; break;
            }

            Str = dt.Year.ToString() + " წ. " + dt.Day.ToString() + " " + Str;
            return Str;
        }



        private void CodexLoadHistoryMenuCaptions(int ID)
        {
         
            history_data_model.Items.Clear();
            
            string strsql = "Select ID, H_Date from CodexDS_DDOCS_History " +
                            $" WHERE ((C_ID = { ID.ToString() } ) AND ( H_STATUS = 0)) Order By H_Date desc";

            if (app.state.RunTimeLicense.IsHistoryExtendedAccessGranted() == true)
                strsql = "Select ID, H_Date from CodexDS_DDOCS_History " +
                             $" WHERE (C_ID = { ID.ToString() } ) AND (( H_STATUS = 0) OR ( H_STATUS = 1)) Order By H_Date desc";


            if (app.state.RunTimeLicense.IsRecoverDeletedDocumentsGranted() == true)
                strsql = "Select ID, H_Date from CodexDS_DDOCS_History " +
                             $" WHERE (C_ID = { ID.ToString() } ) Order By H_Date desc";

            using (var cn = new SqlConnection(app.state.ConnectionString))
            {
                cn.Open();

                using (var command = new SqlCommand(strsql, cn))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            history_data_model.Items.Add(new HistoryMenuModelEntry()
                            {
                                ID = (int)reader["ID"],
                                Caption = HistoryListDateTimetoGString((DateTime)reader["H_Date"]) + " " + ((DateTime)reader["H_Date"]).ToShortTimeString()
                            }

                            );
                        }
                    }
                }
            }

        }





        public void CreateHistoryMenu()
        {
            using (new WaitCursor())
            {
                if (HasDocumentHistory(CodexDocumentID) == true)
                {
                    DSToolBar.Ribbon.Tabs["RCODEX"].Groups["RCodex_History"].Visible = true;
                    DSToolBar.Tools["ისტორია"].SharedProps.Enabled = true;
                    DSToolBar.Tools["ისტორია"].SharedProps.Visible = true;
                    CodexLoadHistoryMenuCaptions(CodexDocumentID);

                    var hmenu = DSToolBar.Tools["ისტორია"] as Infragistics.Win.UltraWinToolbars.PopupMenuTool;
                    hmenu.Tools.Clear();
                    
                    foreach (var item in history_data_model.Items)
                    {
                        String key = "HistoryClick_" + item.ID.ToString();
                        Infragistics.Win.UltraWinToolbars.ButtonTool Button1 = new Infragistics.Win.UltraWinToolbars.ButtonTool(key);
                        Button1.SharedProps.Caption = item.Caption;
                        Button1.Tag = item.ID;
                        Button1.SharedProps.AppearancesSmall.Appearance.Image = Properties.Resources.clock3;
                        if (DSToolBar.Tools.Exists(key) == false) DSToolBar.Tools.Add(Button1);
                        hmenu.Tools.AddTool(key);
                    }

                    
                    if ((DS_DocumentFormat == 0) && app.state.RunTimeLicense.IsDeletedInHistoryGranted() &&  (app.state.RunTimeLicense.IsDocumentViewRestrictedMode() == false))
                    {
                        String Xkey = "HistoryClick_Comparation";
                        Infragistics.Win.UltraWinToolbars.ButtonTool Button_1 = new Infragistics.Win.UltraWinToolbars.ButtonTool(Xkey);
                        Button_1.SharedProps.Caption = "დოკუმენტების შედარება";
                        Button_1.Tag = "დოკუმენტების შედარება";
                        //Button1.SharedProps.AppearancesSmall.Appearance.Image = Properties.Resources.clock3;
                        if (DSToolBar.Tools.Exists(Xkey) == false) DSToolBar.Tools.Add(Button_1);
                        hmenu.Tools.AddTool(Xkey);
                    }

                }
                else
                {
                    DSToolBar.Ribbon.Tabs["RCODEX"].Groups["RCodex_History"].Visible = false;
                    DSToolBar.Tools["ისტორია"].SharedProps.Enabled = false;
                    DSToolBar.Tools["ისტორია"].SharedProps.Visible = false;
                }
            }
        }

        public bool ISDocumentExsists(int ID)
        {
            string strsql = "Select C_ID FROM CODEXDS_DDOCS WHERE C_ID = " + ID.ToString() + " ";
            SqlDataAdapter dacgl = new SqlDataAdapter(strsql, app.state.ConnectionString);
            DataSet dst = new DataSet();
            dacgl.Fill(dst);
            if (dst.Tables[0].Rows.Count == 1) return true; // Document Not Found
            return false;
        }

        private int CodexOpenDocument(int ID, ref string DocFileName, ref string LinkFileName, ref string LinkSchemaFileName)
        {
            HistoryMode = false;
            this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
            // Define Document Call Statment
            String Fields = "C_ID,C_Caption,C_TEXT,C_Link, C_Author,C_Subject,C_Type,C_Words,C_Number,C_Date,C_lastEdit,C_EnterDate,C_Status,C_DocEncoding,C_NumberStr,C_Status,C_Coments,C_Category,C_Addtional,C_Group,C_DocFormat,C_Attach";
            string strsql = "Select "+Fields+ " FROM CODEXDS_DDOCS WHERE C_ID = " + ID.ToString() + " ";
            SqlDataAdapter dacgl = new SqlDataAdapter(strsql, app.state.ConnectionString);
            DataSet dst = new DataSet();
            dacgl.Fill(dst);
            if (dst.Tables[0].Rows.Count != 1) return -1; // Document Not Found

            DS_Comments = dst.Tables[0].Rows[0]["C_Coments"].ToString();
            DS_Additional = dst.Tables[0].Rows[0]["C_Addtional"].ToString();

            if ((int)dst.Tables[0].Rows[0]["C_Group"] != 0)
            {
                if (app.state.RunTimeLicense.IsEnterInConfidentialDocumentAlowed() == false)
                {
                    ILGMessageBox.Show("თქვენ არ გაქვთ უფლება შეხვიდეთ ამ დოკუმენტში");
                    return -1000;
                }
            }

            DS_DocumentFormat = (int)dst.Tables[0].Rows[0]["C_DocFormat"];

            #region OtherFormats
            if ((DS_DocumentFormat != 0) && (DS_DocumentFormat != 1))
            {
                if (DS_DocumentFormat == 2)
                {
                    ILGMessageBox.Show("დოკუმენტი არის HTML ფორმატში, თქვენი სისტემის ვერსია მოძველებულია \n"+
                                       "ამ დოკუმენტის გახსნისთვის თქვენ დაგჭირდებათ  პროგრამის უფრო ახალი ვერსია");
                    return -7701;
                }
                
                if (DS_DocumentFormat == 3)
                {
                    ILGMessageBox.Show("დოკუმენტი არის HTML ფორმატში, თქვენი სისტემის ვერსია მოძველებულია \n" +
                                                         "ამ დოკუმენტის გახსნისთვის თქვენ დაგჭირდებათ  პროგრამის უფრო ახალი ვერსია");
                    return -7701;
                }

                if (DS_DocumentFormat == 4)
                {
                    ILGMessageBox.Show("დოკუმენტი არის HTML ფორმატში, თქვენი სისტემის ვერსია მოძველებულია \n" +
                                                         "ამ დოკუმენტის გახსნისთვის თქვენ დაგჭირდებათ  პროგრამის უფრო ახალი ვერსია");
                    return -7701;
                }

                if (DS_DocumentFormat == 5)
                {
                    ILGMessageBox.Show("დოკუმენტი არის DOCX ფორმატში, თქვენი სისტემის ვერსია მოძველებულია \n" +
                                        "ამ დოკუმენტის გახსნისთვის თქვენ დაგჭირდებათ  პროგრამის უფრო ახალი ვერსია");
                    return -7701;
                }

                if (DS_DocumentFormat == 10)
                {
                    ILGMessageBox.Show("დოკუმენტი არის ODF ფორმატში, თქვენი სისტემის ვერსია მოძველებულია \n" +
                                                         "ამ დოკუმენტის გახსნისთვის თქვენ დაგჭირდებათ  პროგრამის უფრო ახალი ვერსია");
                    return -7701;
                }

                if (DS_DocumentFormat == 20)
                {
                    ILGMessageBox.Show("დოკუმენტი არის XPS ფორმატში, თქვენი სისტემის ვერსია მოძველებულია \n" +
                                                         "ამ დოკუმენტის გახსნისთვის თქვენ დაგჭირდებათ  პროგრამის უფრო ახალი ვერსია");
                    return -7701;
                }

                if (DS_DocumentFormat == 21)
                {
                    ILGMessageBox.Show("დოკუმენტი არის OpenXPS ფორმატში, თქვენი სისტემის ვერსია მოძველებულია \n" +
                                                         "ამ დოკუმენტის გახსნისთვის თქვენ დაგჭირდებათ  პროგრამის უფრო ახალი ვერსია");
                    return -7701;
                }

                if (DS_DocumentFormat == 30)
                {
                    ILGMessageBox.Show("დოკუმენტი არის Djvu ფორმატში, თქვენი სისტემის ვერსია მოძველებულია \n" +
                                                         "ამ დოკუმენტის გახსნისთვის თქვენ დაგჭირდებათ  პროგრამის უფრო ახალი ვერსია");
                    return -7701;
                }
            }
            #endregion OtherFormats

            byte[] doc = (byte[])dst.Tables[0].Rows[0]["C_Text"];
            
            #region  Codex Temp Files
            String U1 = DateTime.Now.Ticks.ToString();
            string CodexDocTempFilename = DirectoryConfiguration.Instance.DSTemporaryDirectory + "\\Codex_" + ID.ToString() + U1;

            while (System.IO.File.Exists(CodexDocTempFilename) == true)
            {
                U1 = DateTime.Now.Ticks.ToString();
                CodexDocTempFilename = DirectoryConfiguration.Instance.DSTemporaryDirectory + "\\Codex_" + ID.ToString() + U1;
            }

            U1 = DateTime.Now.Ticks.ToString();
            string CodexLinkTempFilename = DirectoryConfiguration.Instance.DSTemporaryDirectory + "\\CodexLink_" + ID.ToString() + U1;

            while (System.IO.File.Exists(CodexLinkTempFilename) == true)
            {
                U1 = DateTime.Now.Ticks.ToString();
                CodexLinkTempFilename = DirectoryConfiguration.Instance.DSTemporaryDirectory + "\\CodexLink_" + ID.ToString() + U1;
            }


             #endregion Codex Temp Files

            FileStream fs = new FileStream(CodexDocTempFilename+ ".tmpD", FileMode.Create, FileAccess.Write);
            fs.Write(doc, 0, doc.Length);
            fs.Close();


            doc = (byte[])dst.Tables[0].Rows[0]["C_LINK"];
            fs = new FileStream(CodexLinkTempFilename + ".tmpL", FileMode.Create, FileAccess.Write);
            fs.Write(doc, 0, doc.Length);
            fs.Close();


            if (dst.Tables[0].Rows[0]["C_Attach"] != DBNull.Value)
            {
                doc = (byte[])dst.Tables[0].Rows[0]["C_Attach"];
                fs = new FileStream(CodexLinkTempFilename + ".tmpA", FileMode.Create, FileAccess.Write);
                fs.Write(doc, 0, doc.Length);
                fs.Close();
                HasAttachmen = true;
                ZipFileName = CodexLinkTempFilename + ".tmpA";
            }
            else { HasAttachmen = false; }


            // UnZip Document there
            String ZipFileName2 = CodexDocTempFilename + ".tmpD";
            //C1.C1Zip.C1ZipFile zf = new C1.C1Zip.C1ZipFile();
            //zf.Open(CodexDocTempFilename + ".tmpD");

            if (DS_DocumentFormat == 0)
            {
                DSZip.UnZip(ZipFileName2, "D.RTF", CodexDocTempFilename + ".RTF");
                //zf.Entries.Extract("D.RTF", CodexDocTempFilename + ".RTF");
            }
            else
            {
                DSZip.UnZip(ZipFileName2, "D.PDF", CodexDocTempFilename + ".PDF");
                //zf.Entries.Extract("D.PDF", CodexDocTempFilename + ".PDF");
            }
            //zf.Close();

            // UnZip Links there
            //C1.C1Zip.C1ZipFile zf2 = new C1.C1Zip.C1ZipFile();
            //zf2.Open(CodexLinkTempFilename + ".tmpL");
            ZipFileName2 = CodexLinkTempFilename + ".tmpL";
            DSZip.UnZip(ZipFileName2, "Links.XML", CodexDocTempFilename + "_L.XML");
            DSZip.UnZip(ZipFileName2, "LinksSchema.XML", CodexDocTempFilename + "_LS.XML");

            //zf2.Entries.Extract("Links.XML", CodexDocTempFilename + "_L.XML");
            //zf2.Entries.Extract("LinksSchema.XML", CodexDocTempFilename + "_LS.XML");
            //zf2.Close();

            
                     
            
            
            File.Delete(CodexDocTempFilename + ".tmpD");
            File.Delete(CodexLinkTempFilename + ".tmpL");
            //File.Delete(CodexDocTempFilename + ".zip");

            
            // Document Caption
            if (DS_DocumentFormat == 0)
                DocFileName = CodexDocTempFilename + ".rtf";
            else
                DocFileName = CodexDocTempFilename + ".pdf";

            LinkFileName = CodexDocTempFilename + "_L.XML";
            LinkSchemaFileName = CodexDocTempFilename + "_LS.XML";

            CodexDocumentID = ID; // Document ID
            DS_DocEncoding = dst.Tables[0].Rows[0]["C_DocEncoding"].ToString();//"UNICODE"; //dst.Tables[0].Rows[0]["C_DocEncoding"].ToString();

            // Calcucate Document Caption 
            LockupTables.Tables["CodexDS_DAuthor"].PrimaryKey = new DataColumn[] { LockupTables.Tables["CodexDS_DAuthor"].Columns["A_ID"] };
            LockupTables.Tables["CodexDS_DType"].PrimaryKey = new DataColumn[] { LockupTables.Tables["CodexDS_DType"].Columns["T_ID"] };
            LockupTables.Tables["CodexDS_DStatus"].PrimaryKey = new DataColumn[] { LockupTables.Tables["CodexDS_DStatus"].Columns["C_ID"] };
            LockupTables.Tables["CodexDS_DCategory"].PrimaryKey = new DataColumn[] { LockupTables.Tables["CodexDS_DCategory"].Columns["C_ID"] };


            StringBuilder Strauthor = new StringBuilder("0");
            StringBuilder Strtype = new StringBuilder("0");
            StringBuilder Strstatus = new StringBuilder("0");
            StringBuilder StrCategory = new StringBuilder("0");
            DataRow dr;


            Strauthor.Remove(0, Strauthor.Length);
            Strtype.Remove(0, Strtype.Length);
            Strstatus.Remove(0, Strstatus.Length);
            StrCategory.Remove(0, StrCategory.Length);


            dr = LockupTables.Tables["CodexDS_DAuthor"].Rows.Find((int)dst.Tables[0].Rows[0]["C_Author"]);
            if (dr == null) Strauthor.Append(" "); else Strauthor.Append(@dr["A_Caption"].ToString().Trim());
            
            dr = LockupTables.Tables["CodexDS_DType"].Rows.Find((int)dst.Tables[0].Rows[0]["C_Type"]); 
            if (dr == null) Strtype.Append(" "); else Strtype.Append(@dr["T_Caption"].ToString().Trim());

            // New ITEMS
            dr = LockupTables.Tables["CodexDs_DStatus"].Rows.Find((int)dst.Tables[0].Rows[0]["C_Status"]);

            if (dr == null) Strstatus.Append(" ");
            {
                if (dr["C_ID"].ToString().Trim() == "0") Strstatus.Append(" ");
                else Strstatus.Append(@dr["C_Caption"].ToString().Trim());
            }

            dr = LockupTables.Tables["CodexDs_DCategory"].Rows.Find((int)dst.Tables[0].Rows[0]["C_Category"]);

            if (dr == null) StrCategory.Append(" ");
            {
                if (dr["C_ID"].ToString().Trim() == "0") StrCategory.Append(" ");
                else StrCategory.Append(@dr["C_Caption"].ToString().Trim());
            }

            // -----------------------------------------------------------


            String S =
                PickDate.DateToString((DateTime)dst.Tables[0].Rows[0]["C_Date"]) + "  "
                + Strauthor.ToString() + "  " + Strtype + " ";

            if (dst.Tables[0].Rows[0]["C_numberStr"] != null)
            {
                if (dst.Tables[0].Rows[0]["C_numberStr"].ToString().Trim() != "") S = S + "N " + (dst.Tables[0].Rows[0]["C_numberStr"]).ToString().Trim();
            }


            if (Strstatus.ToString().Trim() != "") S = S + " : <" + Strstatus + "> ";
            if (StrCategory.ToString().Trim() != "") S = S + " : (" + StrCategory + ") ";



            String Saddt = "";
            if (dst.Tables[0].Rows[0]["C_Addtional"] == null) Saddt = "";
            else
            {
                if (dst.Tables[0].Rows[0]["C_Addtional"].ToString().Trim() != "")
                    Saddt = dst.Tables[0].Rows[0]["C_Addtional"].ToString().Trim();
            }

            if (Saddt.Trim() != "") S = S + "  " + Saddt;

            Int32 IDX = -1;
            String IDACCESS = "";
            if (dst.Tables[0].Rows[0]["C_Group"] == null) IDX = -1;
            else
            {
                IDX = (int)dst.Tables[0].Rows[0]["C_ID"];
                if ((int)dst.Tables[0].Rows[0]["C_Group"] == 0)
                {
                    if (app.state.RunTimeLicense.IsDocumentIDShowInList() == true) IDACCESS = " ID = #" + IDX.ToString();
                }
                else
                {
                    if (app.state.RunTimeLicense.IsConfidentialDocumentIDShowInList() == true) IDACCESS = " ID = #" + IDX.ToString();
                }
            }

            if (IDACCESS.Trim() != "") S = S + IDACCESS;


            // -------------------------------

             CodexDocumentCaption = @S;
             DS_DocumentTitle = dst.Tables[0].Rows[0]["C_Caption"].ToString();
             
             this.Cursor = System.Windows.Forms.Cursors.Default;

             
            return 0;
        }
        
  
  

        // Document Operation

        DataSet CodexLink;
        const int MAX_PATH = 260;
        public void CodexLoadDoc(string DocFile,string LinkFile,string LinkSchemaFile, string _TCaption,string _DCaption)
        {
            IntPtr hImgSmall; //the handle to the system image list
            //IntPtr hImgLarge; //the handle to the system image list
            //string fName; //  'the file name to get icon from ZZZ
            SHFILEINFO shinfo = new SHFILEINFO();

            this.Cursor = System.Windows.Forms.Cursors.WaitCursor;

            if (DS_DocumentFormat == 0)
            {
                F_DS_DOC.textControl_Codex.Load(DocFile, TXTextControl.StreamType.RichTextFormat);
                File.Delete(DocFile);
                F_DS_DOC.DocumentTab.SelectedTab = F_DS_DOC.DocumentTab.Tabs[0];

            }
            else
            {

                F_DS_DOC.pdfViewer1.LoadDocument(DocFile);
                
               
                F_DS_DOC.DocumentTab.SelectedTab = F_DS_DOC.DocumentTab.Tabs[1];
                // File.Delete(DocFile);
                if (F_DS_DOC.LoadedPDFDoc != "") {  try { File.Delete(F_DS_DOC.LoadedPDFDoc); } catch { }; }

                F_DS_DOC.LoadedPDFDoc = DocFile;
            }

            CodexLink = new DataSet();
            CodexLink.ReadXmlSchema(LinkSchemaFile);
            CodexLink.ReadXml(LinkFile,System.Data.XmlReadMode.InferSchema);
			File.Delete(LinkFile);




            F_DS_DOC.CodexLinkBox.DataSource = CodexLink.Tables[0].Select("", "ORDER DESC");
            F_DS_DOC.CodexLinkBox.Visited = this.DSVisited;
            F_DS_DOC.CodexLinkBox.InitializeVarialbles(2);
            F_DS_DOC.CodexLinkBox.FillGrid();

            // CodexDocumentCaption = _TCaption; // ამოღებულია რადაგნ დოკუმენტის გამოძახების დროს გენერირდება
            
            if ((HasAttachmen == true) && (app.state.RunTimeLicense.IsAttachmentShow() == true))
            {

                F_DS_DOC.imageList1.Images.Clear();
                F_DS_DOC.listView1.Items.Clear();
                
                ///C1.C1Zip.C1ZipFile zf3 = new C1.C1Zip.C1ZipFile();
                ///zf3.Open(ZipFileName);
                //ZipArchive zipfile = new ZipArchive(new DiskFile(ZipFileName));
                List<string> filesinzip = DSZip.GetEntitiesList(ZipFileName);


                if (filesinzip.Count != 0)
                {
                    foreach (var f in filesinzip)
                    {
                        string st1 = DirectoryConfiguration.Instance.DSTemporaryDirectory + "\\" + Path.GetFileName(f.ToString()).ToUpper().Trim();
                        st1 = st1.Replace("?", "_");

                        string str = Path.GetFileName(f.ToString()).Trim(); //zf3.Entries[ii].FileName.ToString();

                        if (st1.Length >= MAX_PATH)
                        {
                            ILGMessageBox.Show("მიერთებული ფაილის დასახელება არის ოპერაციულ სისტეამში განსაზღვრულ სიგრძეზე დიდი" + System.Environment.NewLine +
                                                f.ToString() + System.Environment.NewLine +
                                                "შეცვალეთ მიერთებული ფაილის სახელი", "",MessageBoxButtons.OK,MessageBoxIcon.Error);
                        }
                        else
                        {
                            DSZip.UnZip(ZipFileName, f, st1);
                            File.SetAttributes(st1, FileAttributes.Normal);
                            hImgSmall = Win32.SHGetFileInfo(st1, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), Win32.SHGFI_ICON | Win32.SHGFI_LARGEICON);
                            File.Delete(st1);
                            //The icon is returned in the hIcon member of the shinfo struct
                            System.Drawing.Icon myIcon = System.Drawing.Icon.FromHandle(shinfo.hIcon);
                            F_DS_DOC.imageList1.Images.Add(str, myIcon);
                        }

                      F_DS_DOC.listView1.Items.Add(str, str);
                      
                  }
                }
                //zf3.Close();
                
                //F_DS_DOC.ultraTabControl1.SelectedTab = F_DS_DOC.ultraTabControl1.Tabs[0];
                F_DS_DOC.ultraDockManager1.ControlPanes[1].Closed = false;
                F_DS_DOC.ultraDockManager1.ControlPanes[1].Activate();
                F_DS_DOC.dockableWindow3.Visible = true;
                F_DS_DOC.dockableWindow3.Enabled = true;
                F_DS_DOC.ultraDockManager1.ControlPanes[0].Activate();
                //F_DS_DOC.ultraTabControl1.Tabs[1].Enabled = true;
                //F_DS_DOC.ultraTabControl1.Tabs[1].Visible = true;

            }
            else
            {
                while (F_DS_DOC.imageList1.Images.Count > 0)
                {
                    F_DS_DOC.imageList1.Images.RemoveAt(0);
                }

                while (F_DS_DOC.listView1.Items.Count > 0)
                {
                    F_DS_DOC.listView1.Items.RemoveAt(0);
                }
                //F_DS_DOC.ultraTabControl1.SelectedTab = F_DS_DOC.ultraTabControl1.Tabs[0];
                //F_DS_DOC.ultraTabControl1.Tabs[1].Enabled = false;
                //F_DS_DOC.ultraTabControl1.Tabs[1].Visible = false;
                
                F_DS_DOC.ultraDockManager1.ControlPanes[1].Closed = true;
                F_DS_DOC.ultraDockManager1.ControlPanes[1].Close();
                F_DS_DOC.dockableWindow3.Visible = false;
                F_DS_DOC.dockableWindow3.Enabled = false;
                F_DS_DOC.ultraDockManager1.ControlPanes[0].Activate();
                
                
            }


            if (DS_Comments.ToString().Trim() == "")
            {
                F_DS_DOC.ultraDockManager1.ControlPanes[2].Closed = true;
                F_DS_DOC.ultraDockManager1.ControlPanes[2].Close();
                F_DS_DOC.dockableWindow4.Visible = false;
                F_DS_DOC.dockableWindow4.Enabled = false;
                F_DS_DOC.ultraDockManager1.ControlPanes[0].Activate();
                

            }
            else
            {
                F_DS_DOC.ultraDockManager1.ControlPanes[2].Closed =false;
                F_DS_DOC.ultraDockManager1.ControlPanes[2].Activate();
                F_DS_DOC.dockableWindow4.Visible = true;
                F_DS_DOC.dockableWindow4.Enabled =true;
                F_DS_DOC.ultraDockManager1.ControlPanes[0].Activate();

                F_DS_DOC.textBoxComments.Text = DS_Comments.TrimEnd();
            }

            
            if (DS_Additional.ToString().Trim() == "")
            {
                F_DS_DOC.ultraDockManager1.ControlPanes[3].Closed = true;
                F_DS_DOC.ultraDockManager1.ControlPanes[3].Close();
                F_DS_DOC.dockableWindow5.Visible = false;
                F_DS_DOC.dockableWindow5.Enabled = false;
                F_DS_DOC.ultraDockManager1.ControlPanes[0].Activate();
                
            }
            else
            {
                F_DS_DOC.ultraDockManager1.ControlPanes[3].Closed = false;
                F_DS_DOC.ultraDockManager1.ControlPanes[3].Activate();
                F_DS_DOC.dockableWindow5.Visible = true;
                F_DS_DOC.dockableWindow5.Enabled = true;
                F_DS_DOC.ultraDockManager1.ControlPanes[0].Activate();
                F_DS_DOC.TextBoxAdditional.Text = DS_Additional.TrimEnd();
            }

            //string strx;

            CreateHistoryMenu();

            ZoomingCodex();
            F_DS_DOC.textControl_Codex.Focus();
            Codex_UpdateStatusBar();
            F_DS_DOC.CodexDocumentCaptionLabel.Text = "   " + CodexDocumentCaption;
            F_DS_DOC.TableLayout_CaptionOvertheDocument.Visible = false;
            codexfindpostion = 0;
            this.Cursor = System.Windows.Forms.Cursors.Default;
			
        }

 
       
        //=================================================================================================
        #region View Layout
        public int CodexViewLayout = 0;
        
        
        void ViewLayout()
        {
            int ChangeView = -1;
                    ChangeView = CodexViewLayout;
                    if (CodexViewLayout == 0)
                    {
                        F_DS_DOC.textControl_Codex.ViewMode = TXTextControl.ViewMode.PageView;
                    }
                    else
                    {
                        F_DS_DOC.textControl_Codex.ViewMode = TXTextControl.ViewMode.Normal;
                        
                    }
                    

           

            if (ChangeView == 0)
            { // Page Layout
 
                this.DSToolBar.ToolClick -= new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.CodexToolBar_ToolClick);
                try
                {
                    (DSToolBar.Tools["NormalLayout"] as Infragistics.Win.UltraWinToolbars.StateButtonTool).Checked = false;
                    (DSToolBar.Tools["PageLayout"] as Infragistics.Win.UltraWinToolbars.StateButtonTool).Checked = true;
                }
                finally
                {
                    this.DSToolBar.ToolClick += new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.CodexToolBar_ToolClick);
                }

                // Page Layout
            }
            else
            { // Normal Layout
                this.DSToolBar.ToolClick -= new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.CodexToolBar_ToolClick);
                try
                {

                    (DSToolBar.Tools["PageLayout"] as Infragistics.Win.UltraWinToolbars.StateButtonTool).Checked = false;
                    (DSToolBar.Tools["NormalLayout"] as Infragistics.Win.UltraWinToolbars.StateButtonTool).Checked = true; ;
                }
                finally
                {
                    this.DSToolBar.ToolClick += new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.CodexToolBar_ToolClick);
                }
            }// Normal Layout
        }

        #endregion View Layout

        #region Zooming

        static public int PixelsPerInch = -1;


        private int detectzoom_Codex(int c)
        {

            if (PixelsPerInch == -1) PixelsPerInch = 96;
            System.Drawing.Graphics g = this.CreateGraphics();
            PixelsPerInch = (int)Math.Round(g.DpiX);

            int PageViewMargin = PixelsPerInch * 567 / 1440;                 /* 567 Twips PageLfet */

            //int W1 = F_CGL_DOC.textControl_CGL.Width - SystemInformation.HorizontalScrollBarThumbWidth - PageViewMargin*2;
            int W1 = F_DS_DOC.textControl_Codex.Width - PageViewMargin * 2;
            int H1 = F_DS_DOC.textControl_Codex.Height;
            //int bottomGap = 400;

            int nPageWidthInPixels = (int)(F_DS_DOC.textControl_Codex.PageSize.Width
                // - F_CGL_DOC.textControl_CGL.PageSize.PageMargins.Left - F_CGL_DOC.textControl_CGL.PageMargins.Right
                                       );
            int nPageHeightInPixels = (int)(F_DS_DOC.textControl_Codex.PageSize.Height
                // - F_CGL_DOC.textControl_CGL.PageMargins.Top - F_CGL_DOC.textControl_CGL.PageMargins.Bottom + bottomGap) 
                                        );


            int nZoomW = (100 * W1) / nPageWidthInPixels;
            int nZoomH = (100 * H1) / nPageHeightInPixels;


            if (nZoomW > 400) nZoomW = 400;
            if (nZoomW < 10) nZoomW = 10;
            if (nZoomH < 10) nZoomH = 10;
            if (nZoomH > 400) nZoomH = 400;

            if (c == 1) return nZoomW;
            return nZoomH;

        }

        
        public int CodexZoomFactor = -20;

        


        public void ZoomingCodex()
        {
            // Zooming 

            if (this.CodexZoomFactor == -20) { F_DS_DOC.textControl_Codex.ZoomFactor = detectzoom_Codex(1); }
            else
            {
                if (CodexZoomFactor == -10) { F_DS_DOC.textControl_Codex.ZoomFactor = detectzoom_Codex(2); }
                else
                {
                    F_DS_DOC.textControl_Codex.ZoomFactor = CodexZoomFactor;
                }
            }

           // F_DS_DOC.CodexDocumentStatusBar.Panels["Zoom"].Text = F_DS_DOC.textControl_Codex.ZoomFactor + "%";
           // F_DS_DOC.CodexZoomTrackBar.Value = F_DS_DOC.textControl_Codex.ZoomFactor;
        }

        // Main Zooming
        public void modify_zoomfactor(int zoom)
        {
            if (F_DS_DOC != null)
            { 
             CodexZoomFactor = zoom; ZoomingCodex(); 
            }
        }

        public void modify_zoomfactor()
        {
            if (F_DS_DOC != null)
            {
                ZoomingCodex();
            }
        }


        #endregion Zooming

        #region Copy & SellectAlL
        void CopyToClipboard()
        {
            if (app.state.RunTimeLicense.IsDocumentViewRestrictedMode() == true)  { ILGMessageBox.Show("მოქმედების ჩატარებისთვის თქვენ არ გაქვთ შესაბამისი უფლება"); return; }
            if (DS_DocumentFormat == 0) F_DS_DOC.textControl_Codex.Copy();
            else {
                if ((F_DS_DOC.pdfViewer1 != null) && (F_DS_DOC.pdfViewer1.Document != null))
                {
                    // ZXZ Not Implemented Yet
                    //////if (F_DS_DOC.pdfViewer1.SelectedText.Length > 0)
                    //////    Clipboard.SetText(F_DS_DOC.pdfViewer1.SelectedText);
                }
            }
        }

        void SelectAll()
        {
            if (app.state.RunTimeLicense.IsDocumentViewRestrictedMode() == true) { ILGMessageBox.Show("მოქმედების ჩატარებისთვის თქვენ არ გაქვთ შესაბამისი უფლება"); return; }
            if (DS_DocumentFormat == 0) F_DS_DOC.textControl_Codex.SelectAll();
            else
            {
                 ////Not Implemented Yet
                 ////ZXZ
                if ((F_DS_DOC.pdfViewer1 != null) && (F_DS_DOC.pdfViewer1.Document != null))
                {
                    F_DS_DOC.pdfViewer1.Renderer.SelectAll();
                }
            }

        }

        #endregion Copy & SellectAlL

        #region Save 
        public void SaveDocCodex()
        {
            if (app.state.RunTimeLicense.IsDocumentViewRestrictedMode() == true) { ILGMessageBox.Show("მოქმედების ჩატარებისთვის თქვენ არ გაქვთ შესაბამისი უფლება"); return; }
            TXTextControl.SaveSettings SaveSettings = new TXTextControl.SaveSettings();
            SaveSettings.PageMargins = F_DS_DOC.textControl_Codex.PageMargins;
            SaveSettings.PageSize = F_DS_DOC.textControl_Codex.PageSize;
            SaveSettings.ImageSaveMode = TXTextControl.ImageSaveMode.SaveAsData;
            SaveFileDialog sd = new SaveFileDialog();

            sd.Filter = sd.Filter = "Microsoft Word (*.docx)|*.docx|Microsoft Word (*.doc)|*.doc|Rich Text Format (*.rtf)|*.rtf|txt files (*.txt)|*.txt";
            sd.FilterIndex = 0;
            sd.RestoreDirectory = true;
            sd.InitialDirectory = DirectoryConfiguration.DSDocumentsDirectory;
            sd.OverwritePrompt = false;
            TXTextControl.StreamType st = new StreamType();
            st = StreamType.RichTextFormat;
            if (sd.ShowDialog() == DialogResult.OK)
            {
                switch (sd.FilterIndex)
                {
                    case 1: st = StreamType.RichTextFormat; break;
                    case 2: st = StreamType.HTMLFormat; break;
                    case 3: st = StreamType.MSWord; break;
                    case 4: st = StreamType.PlainText; break;
                }
                if (File.Exists(sd.FileName) == true)
                {
                    if (ILGMessageBox.Show("თქვენს მიერ მითითებული ფაილი " + Path.GetFileName(sd.FileName) + "\nუკვე არსებობს, გადავაწერო ?", "", System.Windows.Forms.MessageBoxButtons.YesNo,
                        System.Windows.Forms.MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
                        F_DS_DOC.textControl_Codex.Save(sd.FileName, st, SaveSettings);
                }
                else
                {
                    if (sd.ShowDialog() == DialogResult.OK)
                    {
                        string str = System.IO.Path.GetExtension(sd.FileName).Trim().ToUpper();

                        if (str == ".RTF")  { F_DS_DOC.textControl_Codex.Save(sd.FileName, TXTextControl.StreamType.RichTextFormat, SaveSettings); return; }
                        if (str == ".DOC")  { F_DS_DOC.textControl_Codex.Save(sd.FileName, TXTextControl.StreamType.MSWord, SaveSettings); return; }
                        if (str == ".DOCX") { F_DS_DOC.textControl_Codex.Save(sd.FileName, TXTextControl.StreamType.WordprocessingML, SaveSettings); return; }
                        if (str == ".TXT")  { F_DS_DOC.textControl_Codex.Save(sd.FileName, TXTextControl.StreamType.PlainText, SaveSettings); return; }

                    }

                } //F_DS_DOC.textControl_Codex.Save(sd.FileName, st, SaveSettings);

            }

        }


        void SaveDocument()
        {
            SaveDocCodex(); 
        }


        void SaveCommonButton()
        {
                 if (DSTab.SelectedTab == DSTab.Tabs[1]) SaveList();
                        else { if (DSTab.SelectedTab == DSTab.Tabs[2]) SaveDocument(); }
        }




        #endregion Save


        #region Save IN PDF
        public void SaveInPDFCodex()
        {
            if (app.state.RunTimeLicense.IsDocumentViewRestrictedMode() == true) { ILGMessageBox.Show("მოქმედების ჩატარებისთვის თქვენ არ გაქვთ შესაბამისი უფლება"); return; }
            if (DS_DocumentFormat == 0)
            {
                TXTextControl.SaveSettings SaveSettings = new TXTextControl.SaveSettings();
                SaveSettings.PageMargins = F_DS_DOC.textControl_Codex.PageMargins;
                SaveSettings.PageSize = F_DS_DOC.textControl_Codex.PageSize;
                SaveFileDialog sd = new SaveFileDialog();

                sd.Filter = "Portable Document Format (*.pdf)|*.pdf";
                sd.FilterIndex = 0;
                sd.RestoreDirectory = true;
                sd.InitialDirectory = DirectoryConfiguration.DSDocumentsDirectory;
                sd.OverwritePrompt = false;
                TXTextControl.StreamType st = new StreamType();
                st = StreamType.AdobePDF;
                if (sd.ShowDialog() == DialogResult.OK)
                {
                    if (File.Exists(sd.FileName) == true)
                    {
                        if (ILGMessageBox.Show("თქვენს მიერ მითითებული ფაილი " + Path.GetFileName(sd.FileName) + "\nუკვე არსებობს, გადავაწერო ?", "", System.Windows.Forms.MessageBoxButtons.YesNo,
                            System.Windows.Forms.MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
                            F_DS_DOC.textControl_Codex.Save(sd.FileName, st, SaveSettings);
                    }
                    else F_DS_DOC.textControl_Codex.Save(sd.FileName, st, SaveSettings);
                }
            }
            if (DS_DocumentFormat == 1)
            {
                SaveFileDialog sd = new SaveFileDialog();
                sd.Filter = "Portable Document Format (*.pdf)|*.pdf";
                sd.FilterIndex = 0;
                sd.RestoreDirectory = true;
                sd.InitialDirectory = DirectoryConfiguration.DSDocumentsDirectory;
                sd.OverwritePrompt = false;
                TXTextControl.StreamType st = new StreamType();
                st = StreamType.AdobePDF;
                if (sd.ShowDialog() == DialogResult.OK)
                {
                    if (File.Exists(sd.FileName) == true)
                    {
                        if (ILGMessageBox.Show("თქვენს მიერ მითითებული ფაილი " + Path.GetFileName(sd.FileName) + "\nუკვე არსებობს, გადავაწერო ?", "", System.Windows.Forms.MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
                            F_DS_DOC.pdfViewer1.Document.Save(sd.FileName);
                    }
                    else F_DS_DOC.pdfViewer1.Document.Save(sd.FileName);
                }
                
            }

        }


 
  

        public void SaveInPDF()
        {
          SaveInPDFCodex();
        }

        #endregion Save IN PDF

        #region Print Document
        string CodexDocumentCaption = "";
        int CodexDocumentID=0;

        void PrintPDF()
        {
            if (DS_DocumentFormat != 0)
            {
                //PDF Printing
                if ((F_DS_DOC.pdfViewer1 == null) || (F_DS_DOC.pdfViewer1.Document == null)) return;
                F_DS_DOC.pdfViewer1.printButton_Click();
                
                return;
            }
        }


        public void printDoc(bool FullDoc )
        {
            if (DS_DocumentFormat != 0)
            {
                PrintPDF();
                return;
            }


            if (DSBehaviorConfiguration.Instance.content.View.CodexPrintLogic == false)
            {
                F_DS_DOC.textControl_Codex.Print("");
                return;
            }

            byte[] f = new byte[1];
            if (FullDoc == true)
            { // Full Doc
                #region Full Doc
                     F_DS_DOC.textControl_Codex.Save(out f, TXTextControl.BinaryStreamType.MSWord); 
                #endregion Full Doc
            }
            else
            {
                #region Sellection
       
                            if (F_DS_DOC.textControl_Codex.Selection.Length <= 2) { ILGMessageBox.Show("დასაბეჭდი არაფერია"); return; }
                            F_DS_DOC.textControl_Codex.Selection.Save(out f, TXTextControl.BinaryStreamType.MSWord); 
			            
                #endregion Sellection
            }
            
            
            PrintDoc s = new PrintDoc();

            s.Width = 0;
            s.Height = 0;
            s.textControl1.Top = -s.textControl1.Height + 8;
            s.Show();
            s.Visible = false;

            TXTextControl.LoadSettings LoadSettings = new TXTextControl.LoadSettings();
           
 
            s.textControl1.Load(f, TXTextControl.BinaryStreamType.MSWord,LoadSettings);
            // Remove for Version 15 //s.textControl1.PageSize = LoadSettings.PageSize; s.textControl1.PageMargins = LoadSettings.PageMargins; 
            foreach (Section sec in s.textControl1.Sections)
            {
                // Formatinc Document
                if (sec.HeadersAndFooters.Count != 0)
                    sec.HeadersAndFooters.Remove(TXTextControl.HeaderFooterType.All);

                TXTextControl.HeaderFooter Header;
                // Insert headers and footers if the document does not yet contain them
                if (sec.HeadersAndFooters.Count == 0)
                    sec.HeadersAndFooters.Add(TXTextControl.HeaderFooterType.Header);

                s.textControl1.HeaderFooterFrameStyle = HeaderFooterFrameStyle.NoFrame;
                
                // Activate  page header. If there is no first page header, try normal header
                Header = sec.HeadersAndFooters.GetItem(TXTextControl.HeaderFooterType.Header);

                int TableHeaderID_7777 = 7777;
                int TableFooterID_7778 = 7778;

                while (Header.Tables.Add(1, 2, TableHeaderID_7777  /*7777*/ ) == false)
                { TableHeaderID_7777++; }

                Header.Tables.GetItem(TableHeaderID_7777).Cells.GetItem(1, 1).Text = "Codex DS";

                Header.Tables.GetItem(TableHeaderID_7777).Cells.GetItem(1, 2).Text = " /" + s.textControl1.Pages.ToString();
                int p = Header.Tables.GetItem(TableHeaderID_7777).Cells.GetItem(1, 2).Start;
                //Header.Activate();

                Header.Selection.Start = p;
                Header.Selection.Length = 1;
                Header.Selection.ParagraphFormat.Alignment = TXTextControl.HorizontalAlignment.Right;
                Header.Selection.Length = 0;
                Header.PageNumberFields.Add(new PageNumberField(1, TXTextControl.NumberFormat.ArabicNumbers));

                Header.Tables.GetItem(TableHeaderID_7777).Cells.GetItem(1, 1).CellFormat.BottomBorderWidth = 1;
                Header.Tables.GetItem(TableHeaderID_7777).Cells.GetItem(1, 1).CellFormat.RightBorderWidth = 0;
                Header.Tables.GetItem(TableHeaderID_7777).Cells.GetItem(1, 1).CellFormat.LeftBorderWidth = 0;
                Header.Tables.GetItem(TableHeaderID_7777).Cells.GetItem(1, 1).CellFormat.TopBorderWidth = 0;

                Header.Tables.GetItem(TableHeaderID_7777).Cells.GetItem(1, 2).CellFormat.BottomBorderWidth = 1;
                Header.Tables.GetItem(TableHeaderID_7777).Cells.GetItem(1, 2).CellFormat.RightBorderWidth = 0;
                Header.Tables.GetItem(TableHeaderID_7777).Cells.GetItem(1, 2).CellFormat.LeftBorderWidth = 0;
                Header.Tables.GetItem(TableHeaderID_7777).Cells.GetItem(1, 2).CellFormat.TopBorderWidth = 0;

                Header.Deactivate();

                TXTextControl.HeaderFooter Footer;

                if (sec.HeadersAndFooters.Count == 1)
                    sec.HeadersAndFooters.Add(TXTextControl.HeaderFooterType.Footer);
                Footer = sec.HeadersAndFooters.GetItem(TXTextControl.HeaderFooterType.Footer);

                string footerstr = "";
                string SellecionIS = "";
                string FromHistory = "";
                if (HistoryMode == true)
                {
                    FromHistory = HistoryCaption;
                }
         
                if (FullDoc == false) SellecionIS = " ფრაგმენტი";

                        footerstr = " " + CodexDocumentCaption + " ( დაბეჭდილია" + SellecionIS + " : " + PickDate.DateToString(DateTime.Now) + " )" + System.Environment.NewLine+ FromHistory;
          
                while (Footer.Tables.Add(1, 1, TableFooterID_7778  /*7778*/ ) == false)
                { TableFooterID_7778++;  }

                Footer.Tables.GetItem(TableFooterID_7778).Cells.GetItem(1, 1).CellFormat.BottomBorderWidth = 0;
                Footer.Tables.GetItem(TableFooterID_7778).Cells.GetItem(1, 1).CellFormat.RightBorderWidth = 0;
                Footer.Tables.GetItem(TableFooterID_7778).Cells.GetItem(1, 1).CellFormat.LeftBorderWidth = 0;
                Footer.Tables.GetItem(TableFooterID_7778).Cells.GetItem(1, 1).CellFormat.TopBorderWidth = 1;
                Footer.Tables.GetItem(TableFooterID_7778).Cells.GetItem(1, 1).Text = footerstr;

                p = Footer.Tables.GetItem(TableFooterID_7778).Cells.GetItem(1, 1).Start;

                Footer.Activate();
                Footer.Selection.Start = p;
                Footer.Selection.Length = footerstr.Length;
                Footer.Selection.ParagraphFormat.Alignment = TXTextControl.HorizontalAlignment.Left;
                Footer.Selection.FontName = "Sylfaen";
                Footer.Selection.FontSize = 100;
                Footer.Selection.Length = 0;
                Footer.Deactivate();
            }
            if (FullDoc == true)
            {
                s.textControl1.Print("Codex_" + CodexDocumentID.ToString()); 
            }
            else
            {
                 s.textControl1.Print("Codex_Selection" + CodexDocumentID.ToString());
            }



        }


        #endregion Print Document

        #region Show in Word
        public void showinword()
        {
            if (app.state.RunTimeLicense.IsDocumentViewRestrictedMode() == true) { ILGMessageBox.Show("მოქმედების ჩატარებისთვის თქვენ არ გაქვთ შესაბამისი უფლება"); return; }
            string Suffix = "none";
            int DocID = 0;
             Suffix = "CODEX";
                       DocID = CodexDocumentID;

            


            string fn = DirectoryConfiguration.DSDocumentsDirectory + @"\"+Suffix + DocID.ToString();//+".Doc";
            
            int i = 1;
            while (File.Exists(fn + "_" + i.ToString() + ".Doc") == true) { i++; }

            fn = fn + "_" + i.ToString() + ".doc";

            try
            {
                F_DS_DOC.textControl_Codex.Save(fn, TXTextControl.StreamType.MSWord); 
                Process p = new Process();
                p.StartInfo = new ProcessStartInfo(@"file" + @":\\" + fn)
                {
                    UseShellExecute = true
                };
                p.Start();

            }
            catch (Exception ex)
            {
                ILGMessageBox.ShowE("არ ხერხდება დოკუმენტის ექსპორტი MS-Word ში", ex.ToString());
                return;
            }
        }

        public String SaveDocumentFor_ShowInWordRTF()
        {
            String Result = "";

            string Suffix = "DS";
            int DocID = CodexDocumentID;
    
            string fn = DirectoryConfiguration.DSDocumentsDirectory + @"\" + Suffix + DocID.ToString();//+".Doc";

            int i = 1;
            while (File.Exists(fn + "_" + i.ToString() + ".RTF") == true) { i++; }

            fn = fn + "_" + i.ToString() + ".RTF";

            try
            {
               F_DS_DOC.textControl_Codex.Save(fn, TXTextControl.StreamType.RichTextFormat); 
                //Result = @"file" + @":\\" + fn;
                Result = fn.Replace(@"\", @"\\");
            }
            catch (Exception ex)
            {
                MessageBox.Show("არ ხერხდება დოკუმენტის ექსპორტი MS-Word ში\n" + ex.ToString());
                Result = "";
            }


            return Result;

        }


        public void ShowInWordPIA()
        {
            string footerstr_DocumentCaptions = "";
            string footerstr_DocumentDate = "";
            string footerstr_History_ComparationDate = "";

            #region footerstr

           
                        footerstr_DocumentDate = " [ ამოღებულია ბაზიდან " + " : " +PickDate.DateToString(DateTime.Now) + " ]";
                        footerstr_DocumentCaptions = CodexDocumentCaption;
                        footerstr_History_ComparationDate = "";
                        if (HistoryMode == true) footerstr_History_ComparationDate = " ჩატვირთულია არქივიდან ბაზიდან " + HistoryCaption ;
            
            #endregion  footerstr

            String Filename = SaveDocumentFor_ShowInWordRTF();
            if (Filename.Trim() == "") return;

            footerstr_DocumentCaptions = footerstr_DocumentCaptions.Replace("\n", "").Replace("\r", "").Replace(System.Environment.NewLine, "");
            footerstr_DocumentDate = footerstr_DocumentDate.Replace("\n", "").Replace("\r", "").Replace(System.Environment.NewLine, "");
            footerstr_History_ComparationDate = footerstr_History_ComparationDate.Replace("\n", "").Replace("\r", "").Replace(System.Environment.NewLine, "");

            String OutFileName = "";
            if (Path.GetExtension(Filename).ToString() == ".RTF")
            {
                OutFileName = Path.ChangeExtension(Filename, ".docx");
            }
            else return;

            #region OutFileName
            int indexx = 0;
            while (File.Exists(OutFileName) == true)
            {
                indexx++;
                OutFileName = OutFileName.Substring(0, OutFileName.Length - ".docx".Length) + indexx + ".docx";
            }

            #endregion OutFileName


            int restult = 0;
            try
            {
                this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
                restult = DSWordPIA.GenerateOpenDocument_V2(Filename, OutFileName, footerstr_DocumentCaptions, footerstr_DocumentDate, footerstr_History_ComparationDate);
                if (restult != 0) throw new Exception("DS Word PIA ErrorCode:" + restult.ToString());
            }
            catch //(Exception ex)
            {
                this.Cursor = System.Windows.Forms.Cursors.Default;
                showinword();
            }
            finally
            {
                this.Cursor = System.Windows.Forms.Cursors.Default;
            }


            if (File.Exists(OutFileName) == true)
            {
                Process p = new Process();
                p.StartInfo = new ProcessStartInfo(OutFileName)
                {
                    UseShellExecute = true
                };
                p.Start();

            }

            File.Delete(Filename);

        }

        #endregion Show in Word

        #region Change Position

        private void Codex_UpdateStatusBar()
        {
            int allpages = F_DS_DOC.textControl_Codex.Pages;
            int currentpage = F_DS_DOC.textControl_Codex.InputPosition.Page;

            string s1 = "გვერდი  " + currentpage.ToString() + ":  " + currentpage.ToString() + "/" + allpages.ToString();
            //F_DS_DOC.CodexDocumentStatusBar.Panels[0].Text = s1;
            int dline = F_DS_DOC.textControl_Codex.InputPosition.Line;
            int dpos = F_DS_DOC.textControl_Codex.InputPosition.Column;
            s1 = "სტრ. " + dline.ToString() + " პოზ. " + dpos.ToString();
            //F_DS_DOC.CodexDocumentStatusBar.Panels[1].Text = s1;
            //F_DS_DOC.CodexDocumentStatusBar.Panels[2].Text = CodexDocumentCaption;
            this.Text = CodexDocumentCaption;
        
        }

 




        #endregion Change Position

        #region Find in Text

        #region ToolBars
        bool ShowCodexF = false;
        //bool ShowCodexF2 = false;
        public void FindInCodexDocument()
        {
            if (DS_DocumentFormat == 0)
            {
                ShowCodexF = !ShowCodexF;
                F_DS_DOC.CodexDocumentSearchTab.Visible = ShowCodexF;
                F_DS_DOC.CodexDocumentSearchTab.Enabled = ShowCodexF;
                F_DS_DOC.CodexInTextLegacy.Focus();
            }
            else
            {
                //ShowCodexF2 = !ShowCodexF2;
                //F_DS_DOC.CodexDocumentSearchTab2.Visible = ShowCodexF2;
                //F_DS_DOC.CodexDocumentSearchTab2.Enabled = ShowCodexF2;
                //F_DS_DOC.CodexInPDF.Focus();
            }

        }

 
        


        public void FindInDocument()
        {
            F_DS_DOC.FindInDocumentLeftPanel();
          //FindInDSDocument(); 
        }


        void ViewFindButtonStatus()
        {
            bool ChangeFindButtonStatus = false;
            ChangeFindButtonStatus = ShowCodexF; 
            
            if (ChangeFindButtonStatus == false)
            { 

                this.DSToolBar.ToolClick -= new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.CodexToolBar_ToolClick);
                try
                {
                    (DSToolBar.Tools["FindDocument"] as Infragistics.Win.UltraWinToolbars.StateButtonTool).Checked = false;
                }
                finally
                {
                    this.DSToolBar.ToolClick += new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.CodexToolBar_ToolClick);
                }

            }
            else
            { 
                this.DSToolBar.ToolClick -= new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.CodexToolBar_ToolClick);
                try
                {
                    (DSToolBar.Tools["FindDocument"] as Infragistics.Win.UltraWinToolbars.StateButtonTool).Checked = true;
                }
                finally
                {
                    this.DSToolBar.ToolClick += new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.CodexToolBar_ToolClick);
                }
            }
        }

        public char ConvertGeoUnitoGeo8(char c)
        {

            int code = Convert.ToInt16(c);

            if ((code >= 0x10d0) && (code <= 0x10d6)) return Convert.ToChar((code - 0x10d0) + 0xc0);
            if (code == 0x10f1) return Convert.ToChar(0xc7);
            if ((code >= 0x10d7) && (code <= 0x10dc)) return Convert.ToChar((code - 0x10d7) + 0xc8);
            if (code == 0x10f2) return Convert.ToChar(0xce);
            if ((code >= 0x10dd) && (code <= 0x10e2)) return Convert.ToChar((code - 0x10dd) + 0xcf);
            if (code == 0x10f3) return Convert.ToChar(0xd5);
            if ((code >= 0x10e3) && (code <= 0x10ee)) return Convert.ToChar((code - 0x10e3) + 0xd6);
            if (code == 0x10f4) return Convert.ToChar(0xe2);
            if ((code >= 0x10ef) && (code <= 0x10f0)) return Convert.ToChar((code - 0x10ef) + 0xe3);
            if (code == 0x10f5) return Convert.ToChar(0xe5);
            if (code == 0x10f6) return Convert.ToChar(0xe6);
            else return '÷';

        }

        public string GeoUnitoGeo8(string str)
        {
            StringBuilder st = new StringBuilder();

            for (int i = 0; i < str.Length; i++)
            {
                if ((Convert.ToInt16(str[i]) >= 0x10D0) && (Convert.ToInt16(str[i]) <= 0x10FB))
                    st.Append(Convert.ToChar(this.ConvertGeoUnitoGeo8(str[i])));
                else st.Append(str[i]);

            }
            return st.ToString();

        }


        private void ultraTextEditor1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (DSKeyboardLayout.IsActive == true)
            {
                e.KeyChar = DSKeyboardLayout.U[e.KeyChar];
            }
        }


        #endregion ToolBars

        #region Search in Tx Text Control
        // Codex Searching
        int codexfindpostion = 0;
        bool iscodexfff = false;
        public void SearchingInTxText(object sender, EventArgs e)
        {
            string str = GeoUnitoGeo8(F_DS_DOC.CodexInTextLegacy.Text.Trim());
            if (DS_DocEncoding.Trim().ToUpper() == "UNICODE") str = F_DS_DOC.CodexInTextLegacy.Text.Trim();
            if (F_DS_DOC.CodexSerachInCheck.Checked == false) codexfindpostion = F_DS_DOC.textControl_Codex.Find(str, codexfindpostion + 1, TXTextControl.FindOptions.NoMessageBox | TXTextControl.FindOptions.MatchCase);
            else codexfindpostion = F_DS_DOC.textControl_Codex.Find(str, codexfindpostion, TXTextControl.FindOptions.NoMessageBox | TXTextControl.FindOptions.MatchCase | TXTextControl.FindOptions.Reverse);
            if (codexfindpostion == -1)
            {
                if (iscodexfff == true) ILGMessageBox.Show("ტექსტში '" + F_DS_DOC.CodexInTextLegacy.Text.Trim() + "' მეტი არ მოიძებნა ");
                else ILGMessageBox.Show("ტექსტში '" + F_DS_DOC.CodexInTextLegacy.Text.Trim() + "' არ მოიძებნა ");
                iscodexfff = false;
            }
            else iscodexfff = true;


        }
        public void CodexInText_TextChanged(object sender, EventArgs e)
        {
            codexfindpostion = F_DS_DOC.textControl_Codex.InputPosition.TextPosition;
            if (F_DS_DOC.CodexInTextLegacy.Text == "") codexfindpostion = 0;
            iscodexfff = false;

        }

        public void CodexInText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) SearchingInTxText(null, null);
        }


        public void CodexSerachInCheck_CheckedChanged(object sender, EventArgs e)
        {
            //dsfindpostion = 0;
            iscodexfff = false;
        }


        #endregion Search in Tx Text Control

        #region Search in PDF
        
        //List<PdfSearch.FoundText> _foundText = new List<PdfSearch.FoundText>();
        //List<PdfSearch.FoundText> _forHighlight = new List<PdfSearch.FoundText>();
        //Timer _searchTimer;
        //int _searchPageIndex;
        //string _searchText;
        //FindFlags _searchFlags = FindFlags.None;
        //int _prevRecord;
        //public FS_RECTF InflateHighlight { get; set; }
        //public Color HighlightColor { get; set; }
        //public Color ActiveRecordColor { get; set; }
        //public void PdfToolStripSearch()
        //{
        //    _searchTimer = new Timer();
        //    _searchTimer.Interval = 1;
        //    //_searchTimer.Tick += _searchTimer_Tick;
        //    HighlightColor = Color.FromArgb(50, 255, 255, 0);
        //    ActiveRecordColor = Color.FromArgb(255, 255, 255, 0);
        //    InflateHighlight = new FS_RECTF(2.0, 3.5, 2.0, 2.0);
        //}




        #endregion Search in PDF



        #endregion Find in Text




        #region DELETE DOCUMENT
        private String GetTSQLForHistory(int mode, int id)
        {
            String SQL = " IF OBJECT_ID(N'CodexDS_DDOCS_History', N'U') IS NOT NULL " + System.Environment.NewLine +
                         " BEGIN " + System.Environment.NewLine +
                         " INSERT INTO CodexDS_DDOCS_History([H_Date], [H_Caption], [H_Title], [H_Status], [H_OperationStatus], [H_ByUser], [C_ID], [C_CAPTION], [C_AUTHOR], [C_Subject], [C_TYPE], [C_WORDS], [C_NUMBER], [C_NumberStr], [C_DATE], [C_LASTEDIT], [C_ENTERDATE], [C_TEXT], [C_LINK], [C_STATUS], [C_DocFormat], [C_DocEncoding], [C_DocText], [C_Coments], [C_Version], [C_Presentation], [C_Original], [C_Attach], [C_Group], [C_Category], [C_Addtional], [C_Picture]) " + System.Environment.NewLine +
                         $" SELECT GETDATE(), '','',{mode} ,0, '', [C_ID], [C_CAPTION], [C_AUTHOR], [C_Subject], [C_TYPE], [C_WORDS], [C_NUMBER], [C_NumberStr], [C_DATE], [C_LASTEDIT], [C_ENTERDATE], [C_TEXT], [C_LINK], [C_STATUS], [C_DocFormat], [C_DocEncoding], [C_DocText], [C_Coments], [C_Version], [C_Presentation], [C_Original], [C_Attach], [C_Group], [C_Category], [C_Addtional], [C_Picture] " + System.Environment.NewLine +
                         "     FROM[CodexDS_DDOCS] " + System.Environment.NewLine +
                         $"WHERE C_ID = {id} " + System.Environment.NewLine +
                         "END ";
            return SQL;
        }

        public void DODELDocument()
        {

            if (app.state.RunTimeLicense.IsDeleteAlowed() == false)
            {
                ILGMessageBox.Show("თქვენ არ გაქვთ უფლება წაშალოთ დოკუმენტი");
                return;
            }
            // Confirm to Save Document
            if (ILGMessageBox.Show("დოკუმენტის წაშლა \nწაშლილი დოკუმენტი აღარ აღდგება", "", System.Windows.Forms.MessageBoxButtons.YesNo,
                MessageBoxIcon.Error,MessageBoxDefaultButton.Button2) != System.Windows.Forms.DialogResult.Yes) return;

            if (ILGMessageBox.Show("დოკუმენტის წაშლა დაადასტურეთ \nწაშლილი დოკუმენტი აღარ აღდგება", "", System.Windows.Forms.MessageBoxButtons.YesNo,
                System.Windows.Forms.MessageBoxIcon.Error, MessageBoxDefaultButton.Button2) != System.Windows.Forms.DialogResult.Yes) return;

            if (ILGMessageBox.Show("დოკუმენტის წაშლა დაადასტურეთ ხელმეორედ \nწაშლილი დოკუმენტი აღარ აღდგება", "", System.Windows.Forms.MessageBoxButtons.YesNo,
                System.Windows.Forms.MessageBoxIcon.Error, MessageBoxDefaultButton.Button2) != System.Windows.Forms.DialogResult.Yes) return;

            if (ILGMessageBox.Show("დოკუმენტის წაშლა დაადასტურეთ კიდევ ერთხელ \nწაშლილი დოკუმენტი აღარ აღდგება", "", System.Windows.Forms.MessageBoxButtons.YesNo,
                System.Windows.Forms.MessageBoxIcon.Error, MessageBoxDefaultButton.Button2) != System.Windows.Forms.DialogResult.Yes) return;

            // DELETE Procedure

            try
            {
                using (new WaitCursor())
                {
                    using (SqlConnection cn = new SqlConnection(app.state.ConnectionString))
                    {
                        cn.Open();
                        using (SqlTransaction tr = cn.BeginTransaction())
                        {


                            #region Create Command "DeleteDoc"
                            String DeleteCommandSQL = "DELETE [dbo].[CodexDS_DDOCS] WHERE ([C_ID] = @Original_C_ID) ";

                            SqlCommand DeleteDoc = new SqlCommand(DeleteCommandSQL, cn, tr);
                            DeleteDoc.Parameters.Add(new SqlParameter("@Original_C_ID", SqlDbType.Int, 4, ParameterDirection.Input, 0, 0, "C_ID", DataRowVersion.Current, false, null, "", "", ""));

                            #endregion Create Command "DeleteDoc"

                            #region History Command
                            String HistoryCommandSQL = GetTSQLForHistory(-16, CodexDocumentID);
                            SqlCommand HistoryCommand = new SqlCommand(HistoryCommandSQL, cn, tr);
                            HistoryCommand.CommandText = HistoryCommandSQL;
                            HistoryCommand.CommandType = CommandType.Text;

                            #endregion



                            DeleteDoc.Parameters["@Original_C_ID"].Value = CodexDocumentID; ;

                            HistoryCommand.ExecuteNonQuery();
                            DeleteDoc.ExecuteNonQuery();


                            #region UpdateInformation

                            int Quantity = 0;
                            // Update information Tables
                            SqlCommand scoount = new SqlCommand("SELECT COUNT(C_ID) FROM CodexDS_DDocs", cn, tr);
                            Quantity = (int)scoount.ExecuteScalar();


                            string dtstr1 = "CONVERT(DATETIME, '" + DateTime.Now.Year.ToString() + @"-" + DateTime.Now.Month.ToString("00") + @"-" + DateTime.Now.Day.ToString("00") + "T00:00:00.000' ,126) ";
                            string StrInfo = "UPDATE CodexDS2007 SET [C_Version] = 50,  [C_Date] = " + dtstr1 + ", [C_CodexDSDocs] = " + Quantity.ToString() + ", " +
                                         "[C_CodexDate] = " + dtstr1 + ",  [C_CodexDSVersion] = 65 WHERE [C_Version] = 50 ";
                            SqlCommand sinfo = new SqlCommand(StrInfo, cn, tr);


                            #endregion UpdateInformation

                            sinfo.ExecuteNonQuery();
                            tr.Commit();


                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ILGMessageBox.ShowE("არ ხერხდება დოკუმენტის წაშლა Error #DD0001", ex.Message.ToString());
                return;

            }


            this.Cursor = System.Windows.Forms.Cursors.Default;
            ILGMessageBox.Show("დოკუმენტი წაშლილია");
            // Detemine Close or not after Saving
            LoadTables();
            DisplayParametersLimited();
            HomeClick();

         
        }
        #endregion DELETE DOCUMENT


        // Load History Document

        public bool ISDocumentExsistsInHistory(int HID)
        {
            string strsql = "Select count(ID) FROM CodexDS_DDOCS_History WHERE ID = " + HID.ToString() + " ";
            bool result = false;
            using (var cn = new SqlConnection(app.state.ConnectionString))
            {
                cn.Open();
                SqlCommand cm = new SqlCommand(strsql, cn);
                result = ((int)cm.ExecuteScalar() > 0);
            }
            return result;
        }

        private int HistoryCodexOpenDocument(int hID, ref string DocFileName, ref string LinkFileName, ref string LinkSchemaFileName, ref string HCaption)
        {

            this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
            // Define Document Call Statment
            String Fields = "id, H_Date, C_ID,C_Caption,C_TEXT,C_Link, C_Author,C_Subject,C_Type,C_Words,C_Number,C_Date,C_lastEdit,C_EnterDate,C_Status,C_DocEncoding,C_NumberStr,C_Status,C_Coments,C_Category,C_Addtional,C_Group,C_DocFormat,C_Attach";
            string strsql = "Select " + Fields + " FROM CodexDS_DDOCS_History WHERE ID = " + hID.ToString() + " ";
            SqlDataAdapter dacgl = new SqlDataAdapter(strsql, app.state.ConnectionString);
            DataSet dst = new DataSet();
            dacgl.Fill(dst);
            if (dst.Tables[0].Rows.Count != 1) return -1; // Document Not Found

            DS_Comments = dst.Tables[0].Rows[0]["C_Coments"].ToString();
            DS_Additional = dst.Tables[0].Rows[0]["C_Addtional"].ToString();

            if ((int)dst.Tables[0].Rows[0]["C_Group"] != 0)
            {
                if (app.state.RunTimeLicense.IsEnterInConfidentialDocumentAlowed() == false)
                {
                    ILGMessageBox.Show("თქვენ არ გაქვთ უფლება შეხვიდეთ ამ დოკუმენტში");
                    return -1000;
                }
            }

            DS_DocumentFormat = (int)dst.Tables[0].Rows[0]["C_DocFormat"];

            #region OtherFormats
            if ((DS_DocumentFormat != 0) && (DS_DocumentFormat != 1))
            {
                if (DS_DocumentFormat == 2)
                {
                    ILGMessageBox.Show("დოკუმენტი არის HTML ფორმატში, თქვენი სისტემის ვერსია მოძველებულია \n" +
                                       "ამ დოკუმენტის გახსნისთვის თქვენ დაგჭირდებათ  პროგრამის უფრო ახალი ვერსია");
                    return -7701;
                }

                if (DS_DocumentFormat == 3)
                {
                    ILGMessageBox.Show("დოკუმენტი არის HTML ფორმატში, თქვენი სისტემის ვერსია მოძველებულია \n" +
                                       "ამ დოკუმენტის გახსნისთვის თქვენ დაგჭირდებათ  პროგრამის უფრო ახალი ვერსია");
                    return -7701;
                }

                if (DS_DocumentFormat == 4)
                {
                    ILGMessageBox.Show("დოკუმენტი არის HTML ფორმატში, თქვენი სისტემის ვერსია მოძველებულია \n" +
                                                         "ამ დოკუმენტის გახსნისთვის თქვენ დაგჭირდებათ  პროგრამის უფრო ახალი ვერსია");
                    return -7701;
                }

                if (DS_DocumentFormat == 5)
                {
                    ILGMessageBox.Show("დოკუმენტი არის DOCX ფორმატში, თქვენი სისტემის ვერსია მოძველებულია \n" +
                                                         "ამ დოკუმენტის გახსნისთვის თქვენ დაგჭირდებათ  პროგრამის უფრო ახალი ვერსია");
                    return -7701;
                }

                if (DS_DocumentFormat == 10)
                {
                    ILGMessageBox.Show("დოკუმენტი არის ODF ფორმატში, თქვენი სისტემის ვერსია მოძველებულია \n" +
                                                         "ამ დოკუმენტის გახსნისთვის თქვენ დაგჭირდებათ  პროგრამის უფრო ახალი ვერსია");
                    return -7701;
                }

                if (DS_DocumentFormat == 20)
                {
                    ILGMessageBox.Show("დოკუმენტი არის XPS ფორმატში, თქვენი სისტემის ვერსია მოძველებულია \n" +
                                                         "ამ დოკუმენტის გახსნისთვის თქვენ დაგჭირდებათ  პროგრამის უფრო ახალი ვერსია");
                    return -7701;
                }

                if (DS_DocumentFormat == 21)
                {
                    ILGMessageBox.Show("დოკუმენტი არის OpenXPS ფორმატში, თქვენი სისტემის ვერსია მოძველებულია \n" +
                                                         "ამ დოკუმენტის გახსნისთვის თქვენ დაგჭირდებათ  პროგრამის უფრო ახალი ვერსია");
                    return -7701;
                }

                if (DS_DocumentFormat == 30)
                {
                    ILGMessageBox.Show("დოკუმენტი არის Djvu ფორმატში, თქვენი სისტემის ვერსია მოძველებულია \n" +
                                                         "ამ დოკუმენტის გახსნისთვის თქვენ დაგჭირდებათ  პროგრამის უფრო ახალი ვერსია");
                    return -7701;
                }
            }
            #endregion OtherFormats

            byte[] doc = (byte[])dst.Tables[0].Rows[0]["C_Text"];

            #region  Codex Temp Files
            String U1 = DateTime.Now.Ticks.ToString();
            string CodexDocTempFilename = DirectoryConfiguration.Instance.DSTemporaryDirectory + "\\HCodex_" + hID.ToString() + U1;

            while (System.IO.File.Exists(CodexDocTempFilename) == true)
            {
                U1 = DateTime.Now.Ticks.ToString();
                CodexDocTempFilename = DirectoryConfiguration.Instance.DSTemporaryDirectory + "\\HCodex_" + hID.ToString() + U1;
            }

            U1 = DateTime.Now.Ticks.ToString();
            string CodexLinkTempFilename = DirectoryConfiguration.Instance.DSTemporaryDirectory + "\\HCodexLink_" + hID.ToString() + U1;

            while (System.IO.File.Exists(CodexLinkTempFilename) == true)
            {
                U1 = DateTime.Now.Ticks.ToString();
                CodexLinkTempFilename = DirectoryConfiguration.Instance.DSTemporaryDirectory + "\\HCodexLink_" + hID.ToString() + U1;
            }


            #endregion Codex Temp Files

            FileStream fs = new FileStream(CodexDocTempFilename + ".tmpD", FileMode.Create, FileAccess.Write);
            fs.Write(doc, 0, doc.Length);
            fs.Close();


            doc = (byte[])dst.Tables[0].Rows[0]["C_LINK"];
            fs = new FileStream(CodexLinkTempFilename + ".tmpL", FileMode.Create, FileAccess.Write);
            fs.Write(doc, 0, doc.Length);
            fs.Close();


            if (dst.Tables[0].Rows[0]["C_Attach"] != DBNull.Value)
            {
                doc = (byte[])dst.Tables[0].Rows[0]["C_Attach"];
                fs = new FileStream(CodexLinkTempFilename + ".tmpA", FileMode.Create, FileAccess.Write);
                fs.Write(doc, 0, doc.Length);
                fs.Close();
                HasAttachmen = true;
                ZipFileName = CodexLinkTempFilename + ".tmpA";
            }
            else { HasAttachmen = false; }


            // UnZip Document there
            String ZipFileName2 = CodexDocTempFilename + ".tmpD";
            //C1.C1Zip.C1ZipFile zf = new C1.C1Zip.C1ZipFile();
            //zf.Open(CodexDocTempFilename + ".tmpD");

            if (DS_DocumentFormat == 0)
            {
                DSZip.UnZip(ZipFileName2, "D.RTF", CodexDocTempFilename + ".RTF");
                //zf.Entries.Extract("D.RTF", CodexDocTempFilename + ".RTF");
            }
            else
            {
                DSZip.UnZip(ZipFileName2, "D.PDF", CodexDocTempFilename + ".PDF");
                //zf.Entries.Extract("D.PDF", CodexDocTempFilename + ".PDF");
            }
            //zf.Close();

            // UnZip Links there
            //C1.C1Zip.C1ZipFile zf2 = new C1.C1Zip.C1ZipFile();
            //zf2.Open(CodexLinkTempFilename + ".tmpL");
            ZipFileName2 = CodexLinkTempFilename + ".tmpL";
            DSZip.UnZip(ZipFileName2, "Links.XML", CodexDocTempFilename + "_L.XML");
            DSZip.UnZip(ZipFileName2, "LinksSchema.XML", CodexDocTempFilename + "_LS.XML");

            //zf2.Entries.Extract("Links.XML", CodexDocTempFilename + "_L.XML");
            //zf2.Entries.Extract("LinksSchema.XML", CodexDocTempFilename + "_LS.XML");
            //zf2.Close();





            File.Delete(CodexDocTempFilename + ".tmpD");
            File.Delete(CodexLinkTempFilename + ".tmpL");
            //File.Delete(CodexDocTempFilename + ".zip");


            // Document Caption
            if (DS_DocumentFormat == 0)
                DocFileName = CodexDocTempFilename + ".rtf";
            else
                DocFileName = CodexDocTempFilename + ".pdf";

            LinkFileName = CodexDocTempFilename + "_L.XML";
            LinkSchemaFileName = CodexDocTempFilename + "_LS.XML";

           // CodexDocumentID = ID; // Document ID
            DS_DocEncoding = dst.Tables[0].Rows[0]["C_DocEncoding"].ToString();//"UNICODE"; //dst.Tables[0].Rows[0]["C_DocEncoding"].ToString();

            // Calcucate Document Caption 
            LockupTables.Tables["CodexDS_DAuthor"].PrimaryKey = new DataColumn[] { LockupTables.Tables["CodexDS_DAuthor"].Columns["A_ID"] };
            LockupTables.Tables["CodexDS_DType"].PrimaryKey = new DataColumn[] { LockupTables.Tables["CodexDS_DType"].Columns["T_ID"] };
            LockupTables.Tables["CodexDS_DStatus"].PrimaryKey = new DataColumn[] { LockupTables.Tables["CodexDS_DStatus"].Columns["C_ID"] };
            LockupTables.Tables["CodexDS_DCategory"].PrimaryKey = new DataColumn[] { LockupTables.Tables["CodexDS_DCategory"].Columns["C_ID"] };


            StringBuilder Strauthor = new StringBuilder("0");
            StringBuilder Strtype = new StringBuilder("0");
            StringBuilder Strstatus = new StringBuilder("0");
            StringBuilder StrCategory = new StringBuilder("0");
            DataRow dr;


            Strauthor.Remove(0, Strauthor.Length);
            Strtype.Remove(0, Strtype.Length);
            Strstatus.Remove(0, Strstatus.Length);
            StrCategory.Remove(0, StrCategory.Length);


            dr = LockupTables.Tables["CodexDS_DAuthor"].Rows.Find((int)dst.Tables[0].Rows[0]["C_Author"]);
            if (dr == null) Strauthor.Append(" "); else Strauthor.Append(@dr["A_Caption"].ToString().Trim());

            dr = LockupTables.Tables["CodexDS_DType"].Rows.Find((int)dst.Tables[0].Rows[0]["C_Type"]);
            if (dr == null) Strtype.Append(" "); else Strtype.Append(@dr["T_Caption"].ToString().Trim());

            // New ITEMS
            dr = LockupTables.Tables["CodexDs_DStatus"].Rows.Find((int)dst.Tables[0].Rows[0]["C_Status"]);

            if (dr == null) Strstatus.Append(" ");
            {
                if (dr["C_ID"].ToString().Trim() == "0") Strstatus.Append(" ");
                else Strstatus.Append(@dr["C_Caption"].ToString().Trim());
            }

            dr = LockupTables.Tables["CodexDs_DCategory"].Rows.Find((int)dst.Tables[0].Rows[0]["C_Category"]);

            if (dr == null) StrCategory.Append(" ");
            {
                if (dr["C_ID"].ToString().Trim() == "0") StrCategory.Append(" ");
                else StrCategory.Append(@dr["C_Caption"].ToString().Trim());
            }

            // -----------------------------------------------------------


            String S =
                PickDate.DateToString((DateTime)dst.Tables[0].Rows[0]["C_Date"]) + "  "
                + Strauthor.ToString() + "  " + Strtype + " ";

            if (dst.Tables[0].Rows[0]["C_numberStr"] != null)
            {
                if (dst.Tables[0].Rows[0]["C_numberStr"].ToString().Trim() != "") S = S + "N " + (dst.Tables[0].Rows[0]["C_numberStr"]).ToString().Trim();
            }


            if (Strstatus.ToString().Trim() != "") S = S + " : <" + Strstatus + "> ";
            if (StrCategory.ToString().Trim() != "") S = S + " : (" + StrCategory + ") ";



            String Saddt = "";
            if (dst.Tables[0].Rows[0]["C_Addtional"] == null) Saddt = "";
            else
            {
                if (dst.Tables[0].Rows[0]["C_Addtional"].ToString().Trim() != "")
                    Saddt = dst.Tables[0].Rows[0]["C_Addtional"].ToString().Trim();
            }

            if (Saddt.Trim() != "") S = S + "  " + Saddt;

            Int32 IDX = -1;
            String IDACCESS = "";
            if (dst.Tables[0].Rows[0]["C_Group"] == null) IDX = -1;
            else
            {
                IDX = (int)dst.Tables[0].Rows[0]["C_ID"];
                if ((int)dst.Tables[0].Rows[0]["C_Group"] == 0)
                {
                    if (app.state.RunTimeLicense.IsDocumentIDShowInList() == true) IDACCESS = " ID = #" + IDX.ToString();
                }
                else
                {
                    if (app.state.RunTimeLicense.IsConfidentialDocumentIDShowInList() == true) IDACCESS = " ID = #" + IDX.ToString();
                }
            }

            if (IDACCESS.Trim() != "") S = S + IDACCESS;


            // -------------------------------

            CodexDocumentCaption = @S;
            DS_DocumentTitle = dst.Tables[0].Rows[0]["C_Caption"].ToString();

            HCaption = HistoryListDateTimetoGString((DateTime)dst.Tables[0].Rows[0]["H_Date"]) + " " + ((DateTime)dst.Tables[0].Rows[0]["H_Date"]).ToShortTimeString();


            Cursor = System.Windows.Forms.Cursors.Default;


            return 0;
        }

        String HistoryCaption = "";
        public void HistoryCodexLoadDoc(string DocFile, string LinkFile, string LinkSchemaFile, string HCaption)
        {

            HistoryMode = true;
            HistoryCaption = HCaption;


            IntPtr hImgSmall; //the handle to the system image list
            //IntPtr hImgLarge; //the handle to the system image list
            //string fName; //  'the file name to get icon from ZZZ
            SHFILEINFO shinfo = new SHFILEINFO();

            this.Cursor = System.Windows.Forms.Cursors.WaitCursor;

            if (DS_DocumentFormat == 0)
            {
                F_DS_DOC.textControl_Codex.Load(DocFile, TXTextControl.StreamType.RichTextFormat);
                File.Delete(DocFile);
                F_DS_DOC.DocumentTab.SelectedTab = F_DS_DOC.DocumentTab.Tabs[0];

            }
            else
            {
                F_DS_DOC.pdfViewer1.LoadDocument(DocFile);
                //File.Delete(DocFile);
                if (F_DS_DOC.LoadedPDFDoc != "") { try { File.Delete(F_DS_DOC.LoadedPDFDoc); } catch { }; }
                F_DS_DOC.LoadedPDFDoc = DocFile;
                F_DS_DOC.DocumentTab.SelectedTab = F_DS_DOC.DocumentTab.Tabs[1];


            }
            CodexLink = new DataSet();
            CodexLink.ReadXmlSchema(LinkSchemaFile);
            CodexLink.ReadXml(LinkFile, System.Data.XmlReadMode.InferSchema);
            File.Delete(LinkFile);




            F_DS_DOC.CodexLinkBox.DataSource = CodexLink.Tables[0].Select("", "ORDER DESC");
            F_DS_DOC.CodexLinkBox.Visited = this.DSVisited;
            F_DS_DOC.CodexLinkBox.InitializeVarialbles(2);
            F_DS_DOC.CodexLinkBox.FillGrid();

            // CodexDocumentCaption = _TCaption; // ამოღებულია რადაგნ დოკუმენტის გამოძახების დროს გენერირდება

            if ((HasAttachmen == true) && (app.state.RunTimeLicense.IsAttachmentShow() == true))
            {

                F_DS_DOC.imageList1.Images.Clear();
                F_DS_DOC.listView1.Items.Clear();

                
                List<string> filesinzip = DSZip.GetEntitiesList(ZipFileName);


                if (filesinzip.Count != 0)
                {
                    foreach (var f in filesinzip)
                    {
                        string st1 = DirectoryConfiguration.Instance.DSTemporaryDirectory + "\\" + Path.GetFileName(f.ToString()).ToUpper().Trim();
                        st1 = st1.Replace("?", "_");

                        string str = Path.GetFileName(f.ToString()).Trim(); //zf3.Entries[ii].FileName.ToString();

                        if (st1.Length >= MAX_PATH)
                        {
                            ILGMessageBox.Show("მიერთებული ფაილის დასახელება არის ოპერაციულ სისტეამში განსაზღვრულ სიგრძეზე დიდი" + System.Environment.NewLine +
                                                f.ToString() + System.Environment.NewLine +
                                                "შეცვალეთ მიერთებული ფაილის სახელი", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            DSZip.UnZip(ZipFileName, f, st1);
                            File.SetAttributes(st1, FileAttributes.Normal);
                            hImgSmall = Win32.SHGetFileInfo(st1, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), Win32.SHGFI_ICON | Win32.SHGFI_LARGEICON);
                            File.Delete(st1);
                            //The icon is returned in the hIcon member of the shinfo struct
                            System.Drawing.Icon myIcon = System.Drawing.Icon.FromHandle(shinfo.hIcon);
                            F_DS_DOC.imageList1.Images.Add(str, myIcon);
                        }

                        F_DS_DOC.listView1.Items.Add(str, str);

                    }
                }
                //zf3.Close();

                //F_DS_DOC.ultraTabControl1.SelectedTab = F_DS_DOC.ultraTabControl1.Tabs[0];
                F_DS_DOC.ultraDockManager1.ControlPanes[1].Closed = false;
                F_DS_DOC.ultraDockManager1.ControlPanes[1].Activate();
                F_DS_DOC.dockableWindow3.Visible = true;
                F_DS_DOC.dockableWindow3.Enabled = true;
                F_DS_DOC.ultraDockManager1.ControlPanes[0].Activate();
                //F_DS_DOC.ultraTabControl1.Tabs[1].Enabled = true;
                //F_DS_DOC.ultraTabControl1.Tabs[1].Visible = true;

            }
            else
            {
                while (F_DS_DOC.imageList1.Images.Count > 0)
                {
                    F_DS_DOC.imageList1.Images.RemoveAt(0);
                }

                while (F_DS_DOC.listView1.Items.Count > 0)
                {
                    F_DS_DOC.listView1.Items.RemoveAt(0);
                }
                //F_DS_DOC.ultraTabControl1.SelectedTab = F_DS_DOC.ultraTabControl1.Tabs[0];
                //F_DS_DOC.ultraTabControl1.Tabs[1].Enabled = false;
                //F_DS_DOC.ultraTabControl1.Tabs[1].Visible = false;

                F_DS_DOC.ultraDockManager1.ControlPanes[1].Closed = true;
                F_DS_DOC.ultraDockManager1.ControlPanes[1].Close();
                F_DS_DOC.dockableWindow3.Visible = false;
                F_DS_DOC.dockableWindow3.Enabled = false;
                F_DS_DOC.ultraDockManager1.ControlPanes[0].Activate();


            }


            if (DS_Comments.ToString().Trim() == "")
            {
                F_DS_DOC.ultraDockManager1.ControlPanes[2].Closed = true;
                F_DS_DOC.ultraDockManager1.ControlPanes[2].Close();
                F_DS_DOC.dockableWindow4.Visible = false;
                F_DS_DOC.dockableWindow4.Enabled = false;
                F_DS_DOC.ultraDockManager1.ControlPanes[0].Activate();


            }
            else
            {
                F_DS_DOC.ultraDockManager1.ControlPanes[2].Closed = false;
                F_DS_DOC.ultraDockManager1.ControlPanes[2].Activate();
                F_DS_DOC.dockableWindow4.Visible = true;
                F_DS_DOC.dockableWindow4.Enabled = true;
                F_DS_DOC.ultraDockManager1.ControlPanes[0].Activate();

                F_DS_DOC.textBoxComments.Text = DS_Comments.TrimEnd();
            }


            if (DS_Additional.ToString().Trim() == "")
            {
                F_DS_DOC.ultraDockManager1.ControlPanes[3].Closed = true;
                F_DS_DOC.ultraDockManager1.ControlPanes[3].Close();
                F_DS_DOC.dockableWindow5.Visible = false;
                F_DS_DOC.dockableWindow5.Enabled = false;
                F_DS_DOC.ultraDockManager1.ControlPanes[0].Activate();

            }
            else
            {
                F_DS_DOC.ultraDockManager1.ControlPanes[3].Closed = false;
                F_DS_DOC.ultraDockManager1.ControlPanes[3].Activate();
                F_DS_DOC.dockableWindow5.Visible = true;
                F_DS_DOC.dockableWindow5.Enabled = true;
                F_DS_DOC.ultraDockManager1.ControlPanes[0].Activate();
                F_DS_DOC.TextBoxAdditional.Text = DS_Additional.TrimEnd();
            }

            //string strx;

            CreateHistoryMenu();

            ZoomingCodex();
            F_DS_DOC.textControl_Codex.Focus();
            Codex_UpdateStatusBar();
            F_DS_DOC.CodexDocumentCaptionLabel.Text = "   " + CodexDocumentCaption;
            F_DS_DOC.TableLayout_CaptionOvertheDocument.Visible = false;
            codexfindpostion = 0;
            this.Cursor = System.Windows.Forms.Cursors.Default;

            F_DS_DOC.TableLayout_CaptionOvertheDocument.Visible = true;
            F_DS_DOC.CodexDocumentCaptionLabel.Text = $"დოკუმენტი ჩატვირთულია არქივიდან {HCaption}";

        }


        public void CallHistory(int hId)
        {
            if (ISDocumentExsistsInHistory(hId) == false)
            {
                ILGMessageBox.Show("დოკუმენტი ბაზაში არ მოიძებნა");
                
                return;
            }
            string doc = "";
            string link = "";
            string linkscema = "";
            string hCaption = "";
            int ZX = HistoryCodexOpenDocument(hId, ref doc, ref link, ref linkscema, ref hCaption);
            if (ZX == -1000) return;
            if (ZX == -7701) return;
            HistoryCodexLoadDoc(doc, link, linkscema,hCaption);// e._TCaption, e._DCaption);
            DeterminePDFRTFMenu();
            DSToolBar.Tools["ExportToCodex"].SharedProps.Enabled = false;
            DSToolBar.Tools["ChangeDocument"].SharedProps.Enabled = false;
            DSToolBar.Tools["AddToFavorites"].SharedProps.Enabled = false;
            DSToolBar.Tools["DeleteDocument"].SharedProps.Enabled = false;

        }

        public void BackToOriginalDocument()
        {
            if (ISDocumentExsists(CodexDocumentID) == false)
            {
                ILGMessageBox.Show("დოკუმენტი ბაზაში არ მოიძებნა");

                return;
            }
            string doc = "";
            string link = "";
            string linkscema = "";
            int ZX = CodexOpenDocument(CodexDocumentID, ref doc, ref link, ref linkscema);
            if (ZX == -1000) return;
            if (ZX == -7701) return;
            CodexLoadDoc(doc, link, linkscema,DS_DocumentTitle, CodexDocumentCaption);// e._TCaption, e._DCaption);
            DeterminePDFRTFMenu();

            DSToolBar.Tools["ExportToCodex"].SharedProps.Enabled = true;
            DSToolBar.Tools["AddToFavorites"].SharedProps.Enabled = true;

            if (app.state.RunTimeLicense.IsDocumentEditAllow() == true)
            {
                DSToolBar.Tools["ChangeDocument"].SharedProps.Enabled = true;
                DSToolBar.Tools["ChangeDocument"].SharedProps.Visible = true;
            }
            else
            {
                DSToolBar.Tools["ChangeDocument"].SharedProps.Enabled = false;
                DSToolBar.Tools["ChangeDocument"].SharedProps.Visible = false;
            }

            if (app.state.RunTimeLicense.IsDocumentDeletetAllow() == true)
            {
                DSToolBar.Tools["DeleteDocument"].SharedProps.Enabled = true;
                DSToolBar.Tools["DeleteDocument"].SharedProps.Visible = true;
            }
            else
            {
                DSToolBar.Tools["DeleteDocument"].SharedProps.Enabled = false;
                DSToolBar.Tools["DeleteDocument"].SharedProps.Visible = false;
            }


        }
    }

}
