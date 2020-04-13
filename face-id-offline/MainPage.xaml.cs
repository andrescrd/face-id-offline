using FaceIdOffline.Classifier;
using Plugin.Media;
using Plugin.Media.Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace FaceIdOffline
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private readonly IClassifier offlineClassifier;

        public MainPage()
        {
            InitializeComponent();
            offlineClassifier = DependencyService.Get<IClassifier>();
        }

        private async Task Button_Clicked(object sender, EventArgs e)
        {
            var file = await CrossMedia.Current.PickPhotoAsync();

            await HandlePhoto(file);
        }

        private async Task HandlePhoto(MediaFile file)
        {
            var stream = file.GetStreamWithImageRotatedForExternalStorage();

            var memoryStream = new MemoryStream();

            stream.CopyTo(memoryStream);

            var bytes = memoryStream.ToArray();

            offlineClassifier.ClassificationCompleted += Classifier_ClassificationCompleted;
            await offlineClassifier.Classify(bytes);
        }

        private void Classifier_ClassificationCompleted(object sender, ClassificationEventArgs e)
        {
            var sortedList = e.Predictions.OrderByDescending(x => x.Probability);

            var top = sortedList.First();

            if (top.Probability >= 0.9)
            {
                classify.Text = top.TagName + " - " + top.Probability;
            }
            else
            {
                classify.Text = "nothing to classify";
            }

            var classifier = (IClassifier)sender;
            classifier.ClassificationCompleted -= Classifier_ClassificationCompleted;
        }
    }
}
