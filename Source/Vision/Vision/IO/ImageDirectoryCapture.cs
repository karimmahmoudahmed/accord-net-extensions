﻿using Accord.Extensions.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;
using Accord.Extensions;

namespace Accord.Extensions.Vision
{
   public class ImageDirectoryCapture: CaptureBase, IDisposable
    {
        string[] fileNames = null;
        int fps = 0;

        Func<string, IImage> loader;
        long currentFrame = 0;
        Timer timer;

        #region Initialization

        public ImageDirectoryCapture(string filePath, string extension, Func<string, IImage> loader, int frameDelayMilliseconds = 1, bool useNaturalSorting = true)
        {
            this.loader = loader;
            this.CanPause = true;
            
            string ext = "*." + extension.TrimStart('.', '*');
            DirectoryInfo directoryInfo = new DirectoryInfo(filePath);

            IEnumerable<string> files = null;

            if (useNaturalSorting)
            {
                files = directoryInfo.EnumerateFiles(ext, SearchOption.TopDirectoryOnly)
                        .OrderBy(f => f.Name, new NaturalSortComparer<string>())
                        .Select(f => f.FullName);            
            }
            else
            { 
                 files = from file in directoryInfo.EnumerateFiles(ext, SearchOption.TopDirectoryOnly)
                         select file.FullName;
            }

            this.fileNames = files.ToArray();
            this.fps = frameDelayMilliseconds;
        }

        #endregion

        public override void Start()
        {
            timer = new Timer(fps);
            timer.Elapsed += timer_Elapsed;
            timer.Enabled = true;
        }

        protected override void Pause()
        {
            timer.Enabled = false;
        }

        protected override void Resume()
        {
            timer.Enabled = true;
        }

        public override void Stop()
        {
            timer.Stop();
            currentFrame = 0;
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (currentFrame >= fileNames.Length)
            {
                OnVideoFrame(null, false); /*set buffer to null*/
                return;
            }

            IImage image = loader(fileNames[currentFrame]);
            OnVideoFrame(image, false);

            currentFrame++;
        }

        public override Size VideoSize
        {
            get
            { 
                return buffer.Size;
            }
            set
            {
                //not supported
            }
        }

        public void Dispose()
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }

        ~ImageDirectoryCapture()
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }

        public override long Length
        {
            get
            {
                return fileNames.Length;
            }
        }

        public override long Position
        {
            get
            {
                return currentFrame;
            }
        }

        public override long Seek(long offset, SeekOrigin origin = SeekOrigin.Current)
        {
            long newPosition = 0;
            switch (origin)
            { 
                case SeekOrigin.Begin:
                    newPosition = offset;
                    break;
                case SeekOrigin.Current:
                    newPosition = this.Position + offset;
                    break;
                case SeekOrigin.End:
                    newPosition = this.Length + offset;
                    break;
            }

            currentFrame = System.Math.Min(this.Length, System.Math.Max(0, newPosition));
            return currentFrame;
        }

       public string CurrentImageName
        {
            get { return fileNames[this.Position]; }
       }
    }
}