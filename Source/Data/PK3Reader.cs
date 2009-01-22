
#region ================== Copyright (c) 2007 Pascal vd Heiden

/*
 * Copyright (c) 2007 Pascal vd Heiden, www.codeimp.com
 * This program is released under GNU General Public License
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 */

#endregion

#region ================== Namespaces

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using CodeImp.DoomBuilder.IO;
using ICSharpCode.SharpZipLib.Zip;

#endregion

namespace CodeImp.DoomBuilder.Data
{
	internal sealed class PK3Reader : PK3StructuredReader
	{
		#region ================== Variables

		private DirectoryFilesList files;

		#endregion

		#region ================== Constructor / Disposer

		// Constructor
		public PK3Reader(DataLocation dl) : base(dl)
		{
			General.WriteLogLine("Opening PK3 resource '" + location.location + "'");
			
			// Open the zip file
			ZipInputStream zipstream = OpenPK3File();
			
			// Make list of all files
			List<DirectoryFileEntry> fileentries = new List<DirectoryFileEntry>();
			ZipEntry entry = zipstream.GetNextEntry();
			while(entry != null)
			{
				if(entry.IsFile) fileentries.Add(new DirectoryFileEntry(entry.Name));
				
				// Next
				entry = zipstream.GetNextEntry();
			}

			// Make files list
			files = new DirectoryFilesList(fileentries);

			// Done with the zip file
			zipstream.Close();
			zipstream.Dispose();
			
			// Initialize without path (because we use paths relative to the PK3 file)
			Initialize();

			// We have no destructor
			GC.SuppressFinalize(this);
		}

		// Disposer
		public override void Dispose()
		{
			// Not already disposed?
			if(!isdisposed)
			{
				General.WriteLogLine("Closing PK3 resource '" + location.location + "'");
				
				// Done
				base.Dispose();
			}
		}

		#endregion

		#region ================== Management

		// This opens the zip file for reading
		private ZipInputStream OpenPK3File()
		{
			FileStream filestream = File.Open(location.location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			filestream.Seek(0, SeekOrigin.Begin);
			return new ZipInputStream(filestream);
		}

		#endregion
		
		#region ================== Methods
		
		// This creates an image
		protected override ImageData CreateImage(string name, string filename, bool flat)
		{
			return new PK3FileImage(this, name, filename, flat);
		}

		// This returns true if the specified file exists
		protected override bool FileExists(string filename)
		{
			return files.FileExists(filename);
		}

		// This returns all files in a given directory
		protected override string[] GetAllFiles(string path, bool subfolders)
		{
			return files.GetAllFiles(path, subfolders).ToArray();
		}

		// This returns all files in a given directory that match the given extension
		protected override string[] GetFilesWithExt(string path, string extension, bool subfolders)
		{
			return files.GetAllFiles(path, extension, subfolders).ToArray();
		}

		// This finds the first file that has the specific name, regardless of file extension
		protected override string FindFirstFile(string beginswith, bool subfolders)
		{
			return files.GetFirstFile(beginswith, subfolders);
		}

		// This finds the first file that has the specific name, regardless of file extension
		protected override string FindFirstFile(string path, string beginswith, bool subfolders)
		{
			return files.GetFirstFile(path, beginswith, subfolders);
		}

		// This finds the first file that has the specific name
		protected override string FindFirstFileWithExt(string path, string beginswith, bool subfolders)
		{
			string title = Path.GetFileNameWithoutExtension(beginswith);
			string ext = Path.GetExtension(beginswith);
			if(ext.Length > 1) ext = ext.Substring(1); else ext = "";
			return files.GetFirstFile(path, title, subfolders, ext);
		}

		// This loads an entire file in memory and returns the stream
		// NOTE: Callers are responsible for disposing the stream!
		protected override MemoryStream LoadFile(string filename)
		{
			MemoryStream filedata = null;
			byte[] copybuffer = new byte[4096];

			// Open the zip file
			ZipInputStream zipstream = OpenPK3File();

			ZipEntry entry = zipstream.GetNextEntry();
			while(entry != null)
			{
				if(entry.IsFile)
				{
					string entryname = entry.Name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
					
					// Is this the entry we are looking for?
					if(string.Compare(entryname, filename, true) == 0)
					{
						int expectedsize = (int)entry.Size;
						if(expectedsize < 1) expectedsize = 1024;
						filedata = new MemoryStream(expectedsize);
						int readsize = zipstream.Read(copybuffer, 0, copybuffer.Length);
						while(readsize > 0)
						{
							filedata.Write(copybuffer, 0, readsize);
							readsize = zipstream.Read(copybuffer, 0, copybuffer.Length);
						}
						break;
					}
				}
				
				// Next
				entry = zipstream.GetNextEntry();
			}

			// Done with the zip file
			zipstream.Close();
			zipstream.Dispose();
			
			// Nothing found?
			if(filedata == null)
			{
				throw new FileNotFoundException("Cannot find the file " + filename + " in PK3 file " + location.location + ".");
			}
			else
			{
				return filedata;
			}
		}

		// This creates a temp file for the speciied file and return the absolute path to the temp file
		// NOTE: Callers are responsible for removing the temp file when done!
		protected override string CreateTempFile(string filename)
		{
			// Just copy the file
			string tempfile = General.MakeTempFilename(General.Map.TempPath, "wad");
			MemoryStream filedata = LoadFile(filename);
			File.WriteAllBytes(tempfile, filedata.ToArray());
			filedata.Dispose();
			return tempfile;
		}

		// Public version to load a file
		internal MemoryStream ExtractFile(string filename)
		{
			return LoadFile(filename);
		}
		
		#endregion
	}
}
