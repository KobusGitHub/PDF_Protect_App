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
            var searchIdKey = txtId.Text;

            if (string.IsNullOrEmpty(searchIdKey))
            {
                MessageBox.Show("Invalid ID Key");
                return;
            }

            var searchNameKey = txtEmail.Text;

            if (string.IsNullOrEmpty(searchNameKey))
            {
                MessageBox.Show("Invalid Name Key");
                return;
            }


            if(txtOwnerPassword.Text != txtOwnerPassword2.Text || string.IsNullOrEmpty(txtOwnerPassword.Text))
            {
                MessageBox.Show("Password Invalid");
                return;
            }

            // Delete all files in destination
            DirectoryInfo di = new DirectoryInfo(destPath);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }

            
            try
            {
                var result = this.SplitProtect(filePath, destPath, fileName, searchIdKey, searchNameKey, txtOwnerPassword.Text);
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


        private bool SplitProtect(string path, string destPath, string filename, string searchIdKey, string searchNameKey, string ownerPwd)
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

                var identifier = getIdentifier(page, searchIdKey, searchNameKey);
                if (identifier == null)
                {
                    MessageBox.Show("Search Values Invalid");

                    return false;
                }
                identifiers.Add(identifier);

                outputDocument.AddPage(page);

                // var saveFileName = String.Format("{0}_{1}.pdf", name, idx + 1);
                var saveFileName = String.Format("{0}.pdf", identifier.PersonName);

                outputDocument.Save(destPath + "\\" + saveFileName);
            }


            using (StreamWriter w = File.AppendText(destPath + "\\log.txt"))
            {
                w.WriteLine("Owner Password: '" + ownerPwd + "'");
                w.WriteLine("FileName,Password");

                // Set Password
                foreach (var identifier in identifiers)
                {
                    w.WriteLine(identifier.PersonName + ".pdf,'" + identifier.IdNumber + "'");
                    ProtectFile(destPath + "\\" + identifier.PersonName + ".pdf", ownerPwd, identifier.IdNumber);
                }

            }

            return true;
        }

        private Identifier getIdentifier(PdfPage page, string idKey, string nameKey)
        {
            Identifier identifier = new Identifier();

            for (int index = 0; index < page.Contents.Elements.Count; index++)
            {
                PdfDictionary.PdfStream stream = page.Contents.Elements.GetDictionary(index).Stream;
                var outputText = new PDFTextExtractor().ExtractTextFromPDFBytes(stream.Value);

                var searchIdIndex = outputText.IndexOf(idKey);
                if (searchIdIndex > -1)
                {
                    var id = outputText.Substring(searchIdIndex + idKey.Length, 13);
                    identifier.IdNumber = id;
                    if (identifier.isLoaded)
                    {
                        return identifier;
                    }
                }

                var searchNameIndex = outputText.IndexOf(nameKey);
                if (searchNameIndex > -1)
                {
                    var attIndex = outputText.IndexOf("@");

                    var startIndex = searchNameIndex + nameKey.Count();
                    var endIndex = attIndex - startIndex;
                    var name = outputText.Substring(startIndex, endIndex);
                    identifier.PersonName = name;
                    if (identifier.isLoaded)
                    {
                        return identifier;
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



    }
}
