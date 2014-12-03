﻿using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using L10NSharp;
using Palaso.IO;
using Paratext;
using ProtoScript.Bundle;
using ProtoScript.Dialogs;
using ProtoScript.Properties;
using Canon = ProtoScript.Bundle.Canon;

namespace ProtoScript
{
	public partial class SandboxForm : Form
	{
		private Project m_project;
		private string m_bundleIdFmt;
		private string m_LanguageIdFmt;

		public SandboxForm()
		{
			InitializeComponent();
			HandleStringsLocalized();
		}

		private string ProjectsBaseFolder
		{
			get
			{
				return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
					Program.kCompany, Program.kProduct);
			}
		}

		protected void HandleStringsLocalized()
		{
			m_bundleIdFmt = m_lblBundleId.Text;
			m_LanguageIdFmt = m_lblLanguage.Text;
		}

		private void HandleSelectBundle_Click(object sender, EventArgs e)
		{
			SaveCurrentProject();

			using (var dlg = new SelectProjectDialog())
			{
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					if (Path.GetExtension(dlg.FileName) == Project.kProjectFileExtension)
						LoadProject(dlg.FileName);
					else
						LoadBundle(dlg.FileName);
				}
			}
		}

		private void button2_Click(object sender, EventArgs e)
		{
			LocalizationManager.ShowLocalizationDialogBox("");
		}

		private void SandboxForm_Load(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(Settings.Default.CurrentProject) || !File.Exists(Settings.Default.CurrentProject))
			{
				using (var dlg = new WelcomeDialog())
				{
					if (dlg.ShowDialog() == DialogResult.OK)
						LoadBundle(Settings.Default.CurrentProject);
					else
						UpdateDisplayOfProjectIdInfo();
				}
			}
			else
			{
				LoadProject(Settings.Default.CurrentProject);
			}
		}

		private void LoadProject(string filePath)
		{
			if (!LoadAndHandleApplicationExceptions(() => { m_project = Project.Load(filePath); }))
				m_project = null;
			UpdateDisplayOfProjectIdInfo();
		}

		private void LoadBundle(string bundlePath)
		{
			Bundle.Bundle bundle = null;

			if (!LoadAndHandleApplicationExceptions(() => { bundle = new Bundle.Bundle(bundlePath); }))
			{
				m_project = null;
				UpdateDisplayOfProjectIdInfo();
				return;
			}

			// See if we already have a project for this bundle and open it instead.
			var projFilePath = Project.GetProjectFilePath(ProjectsBaseFolder, bundle.Language, bundle.Id);
			if (File.Exists(projFilePath))
			{
				LoadProject(projFilePath);
				return;
			}
			m_project = new Project(bundle.Metadata);
			UpdateDisplayOfProjectIdInfo();
			m_project.PopulateAndParseBooks(bundle);
		}

		private bool LoadAndHandleApplicationExceptions(Action loadCommand)
		{
			try
			{
				loadCommand();
			}
			catch (ApplicationException ex)
			{
				StringBuilder bldr = new StringBuilder(ex.Message);
				if (ex.InnerException != null)
				{
					bldr.Append(Environment.NewLine);
					bldr.Append(ex.InnerException);
				}

				MessageBox.Show(bldr.ToString(), ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return false;
			}
			return true;
		}

		private void UpdateDisplayOfProjectIdInfo()
		{
			m_lblBundleId.Text = string.Format(m_bundleIdFmt, m_project != null ? m_project.Id : String.Empty);
			m_lblLanguage.Text = string.Format(m_LanguageIdFmt, m_project != null ? m_project.Language : String.Empty);
		}
	
		private void SandboxForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			SaveCurrentProject();
			Settings.Default.Save();
		}

		private void SaveCurrentProject()
		{
			if (m_project != null)
				m_project.Save(ProjectsBaseFolder);
		}

		private void HandleExportToTabSeparated_Click(object sender, EventArgs e)
		{
			var defaultDir = Settings.Default.DefaultExportDirectory;
			if (string.IsNullOrEmpty(defaultDir))
			{
				defaultDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			}

			using (var dlg = new SaveFileDialog())
			{
				dlg.Title = LocalizationManager.GetString("DialogBoxes.ExportDlg.Title", "Export Tab-Delimited Data");
				dlg.OverwritePrompt = true;
				dlg.InitialDirectory = defaultDir;
				dlg.FileName = "MRK.txt";
				dlg.Filter = string.Format("{0} ({1})|{1}|{2} ({3})|{3}",
					LocalizationManager.GetString("DialogBoxes.ExportDlg.TabDelimitedFileTypeLabel", "Tab-delimited files"),
					"*.txt",
					LocalizationManager.GetString("DialogBoxes.FileDlg.AllFilesLabel", "All Files"),
					"*.*");
				dlg.DefaultExt = ".txt";
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					Settings.Default.DefaultExportDirectory = Path.GetDirectoryName(dlg.FileName);
					try
					{
						m_project.ExportTabDelimited(dlg.FileName);
					}
					catch(Exception ex)
					{
						MessageBox.Show(ex.Message, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					}
				}
			}
		}

		/// <summary>
		/// TODO This very rudamentary code is just for developer testing at this point
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_btnLoadSfm_Click(object sender, EventArgs e)
		{
			string sfmFilePath = null;
			using (var dlg = new OpenFileDialog())
			{
				if (dlg.ShowDialog() != DialogResult.OK)
					return;
				sfmFilePath = dlg.FileName;
			}
			string usfmStylesheetPath = Path.Combine(FileLocator.GetDirectoryDistributedWithApplication("sfm"), "usfm.sty");
			var scrStylesheet = new ScrStylesheet(usfmStylesheetPath);
			var book = new UsxDocument(UsfmToUsx.ConvertToXmlDocument(scrStylesheet, File.ReadAllText(sfmFilePath)));
			var metadata = new DblMetadata { id = "sfm" + DateTime.Now.Ticks, language = new DblMetadataLanguage { iso = "zzz" } };
			m_project = new Project(metadata);
			m_project.ParseAndAddBooks(new [] { book }, new ScrStylesheetAdapter(scrStylesheet));
		}

		private void m_btnSettings_Click(object sender, EventArgs e)
		{
			using (var dlg = new ProjectSettingsDialog(m_project))
				dlg.ShowDialog();
		}

	}
}
