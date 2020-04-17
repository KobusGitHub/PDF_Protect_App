using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Security;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PDF_Protect_App
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnBrowseFile_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            this.txtFileSelected.Text = openFileDialog1.FileName;

        }


        private void btnDestFolder_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            this.txtDestFolder.Text = folderBrowserDialog1.SelectedPath;
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            var filePath = Path.GetDirectoryName(this.txtFileSelected.Text) + "\\";

            if (!File.Exists(this.txtFileSelected.Text))
            {
                MessageBox.Show("Invalid File");
                return;
            }

            var destPath = this.txtDestFolder.Text + "\\";

            if (!Directory.Exists(this.txtDestFolder.Text))
            {
                MessageBox.Show("Invalid Directory");
                return;
            }

            var fileName = Path.GetFileName(this.txtFileSelected.Text);
            var searchIdStartKey = txtIdStart.Text;
            var searchIdEndKey = txtIdEnd.Text;

            if (string.IsNullOrEmpty(searchIdStartKey))
            {
                MessageBox.Show("Invalid ID start Key");
                return;
            }


            if (string.IsNullOrEmpty(searchIdEndKey))
            {
                MessageBox.Show("Invalid ID end Key");
                return;
            }

            var searchNameKey = txtEmail.Text;

            if (string.IsNullOrEmpty(searchNameKey))
            {
                MessageBox.Show("Invalid Name Key");
                return;
            }


            if (txtOwnerPassword.Text != txtOwnerPassword2.Text || string.IsNullOrEmpty(txtOwnerPassword.Text))
            {
                MessageBox.Show("Password Invalid");
                return;
            }

            if (ckClear.Checked)
            {
                // Delete all files in destination
                DirectoryInfo di = new DirectoryInfo(destPath);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
            }


            try
            {
                var result = this.SplitProtect(filePath, destPath, fileName, searchIdStartKey, searchIdEndKey, searchNameKey, txtNameSuffix.Text, txtOwnerPassword.Text);
                if (result == true)
                {
                    MessageBox.Show("Files created successfully");
                }
            }
            catch (Exception ex)
            {
                using (StreamWriter w = File.AppendText(destPath + "\\log.txt"))
                {
                    w.WriteLine("Error");
                    w.WriteLine(ex.Message);
                }

                MessageBox.Show("Files created failed");
            }



        }


        private bool SplitProtect(string path, string destPath, string filename, string searchIdStartKey, string searchIdEndKey, string searchNameKey, string fileNameSuffix, string ownerPwd)
        {

            var identifiers = new List<Identifier>();

            var source = Path.Combine(path, filename);

            if (!File.Exists(source))
            {
                MessageBox.Show("File does not exist");
                return false;
            }
            var dest = Path.Combine(destPath, filename);
            File.Copy(source, dest, true);

            // Open the file
            // PdfDocument inputDocument = PdfReader.Open(filename, PdfDocumentOpenMode.Import);
            PdfDocument inputDocument = PdfReader.Open(dest, PdfDocumentOpenMode.Import);

            //string name = Path.GetFileNameWithoutExtension(filename);
            string name = Path.GetFileNameWithoutExtension(dest);
            for (int idx = 0; idx < inputDocument.PageCount; idx++)
            {
                // Create new document
                PdfDocument outputDocument = new PdfDocument();
                outputDocument.Version = inputDocument.Version;
                outputDocument.Info.Title = String.Format("{0}_{1}.pdf", idx + 1, inputDocument.Info.Title);
                outputDocument.Info.Creator = inputDocument.Info.Creator;

                // Add the page and save it
                var page = inputDocument.Pages[idx];

                var identifier = getIdentifier(page, searchIdStartKey, searchIdEndKey, searchNameKey);
                if (identifier == null)
                {
                    MessageBox.Show("Search Values Invalid");

                    return false;
                }
                identifiers.Add(identifier);

                outputDocument.AddPage(page);

                // var saveFileName = String.Format("{0}_{1}.pdf", name, idx + 1);
                var saveFileName = String.Format("{0}{1}.pdf", identifier.PersonName, fileNameSuffix);

                outputDocument.Save(destPath + "\\" + saveFileName);
            }


            using (StreamWriter w = File.AppendText(destPath + "\\log.txt"))
            {
                w.WriteLine("Owner Password: '" + ownerPwd + "'");
                w.WriteLine("FileName,Password");

                // Set Password
                foreach (var identifier in identifiers)
                {
                    w.WriteLine(identifier.PersonName + fileNameSuffix + ".pdf,'" + identifier.IdNumber + "'");
                    ProtectFile(destPath + "\\" + identifier.PersonName + fileNameSuffix + ".pdf", ownerPwd, identifier.IdNumber);
                }

            }

            return true;
        }

        private Identifier getIdentifier(PdfPage page, string idStartKey, string idEndKey, string nameKey)
        {
            Identifier identifier = new Identifier();

            for (int index = 0; index < page.Contents.Elements.Count; index++)
            {
                PdfDictionary.PdfStream stream = page.Contents.Elements.GetDictionary(index).Stream;
                var outputText = new PDFTextExtractor().ExtractTextFromPDFBytes(stream.Value);




                var searchNameIndex = outputText.IndexOf(nameKey);
                if (searchNameIndex > -1)
                {
                    var restOfString = outputText.Substring(searchNameIndex);
                    var attIndex = restOfString.IndexOf("@");
                    attIndex = attIndex + searchNameIndex;


                    var startIndex = searchNameIndex + nameKey.Count();
                    var endIndex = attIndex - startIndex;
                    var name = outputText.Substring(startIndex, endIndex);
                    identifier.PersonName = name;
                    if (identifier.isLoaded)
                    {
                        return identifier;
                    }

                }



                var searchIdIndex = outputText.IndexOf(idStartKey);
                if (searchIdIndex > -1)
                {
                    var endKeyIndex = outputText.IndexOf(idEndKey);

                    if(searchNameIndex < endKeyIndex)
                    {
                        return null;
                    }

                    if (endKeyIndex > -1)
                    {
                        var startIndex = searchIdIndex + idStartKey.Length;
                        var idLength = endKeyIndex - startIndex;

                        var id = outputText.Substring(startIndex, idLength);
                        identifier.IdNumber = id;
                        if (identifier.isLoaded)
                        {
                            return identifier;
                        }
                    }
                }

              

            }
            return null;
        }
        private void ProtectFile(string filenameDest, string ownerPw, string userPw)
        {

            // Open an existing document. Providing an unrequired password is ignored.
            PdfDocument document = PdfReader.Open(filenameDest, "some text");

            PdfSecuritySettings securitySettings = document.SecuritySettings;

            // Setting one of the passwords automatically sets the security level to 
            // PdfDocumentSecurityLevel.Encrypted128Bit.
            securitySettings.UserPassword = userPw;
            securitySettings.OwnerPassword = ownerPw;

            // Don't use 40 bit encryption unless needed for compatibility
            //securitySettings.DocumentSecurityLevel = PdfDocumentSecurityLevel.Encrypted40Bit;

            // Restrict some rights.
            securitySettings.PermitAccessibilityExtractContent = false;
            securitySettings.PermitAnnotations = false;
            securitySettings.PermitAssembleDocument = false;
            securitySettings.PermitExtractContent = false;
            securitySettings.PermitFormsFill = true;
            securitySettings.PermitFullQualityPrint = false;
            securitySettings.PermitModifyDocument = false;
            securitySettings.PermitPrint = true;


            // Save the document...
            document.Save(filenameDest);
            // ...and start a viewer.
            //Process.Start(filenameDest);
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
