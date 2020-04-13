using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using FaceIdOffline.Classifier;
using Java.IO;
using Java.Nio;
using Java.Nio.Channels;
using Xamarin.Forms;

[assembly: Dependency(typeof(FaceIdOffline.Droid.Tensorflow.TensorflowClassifier))]
namespace FaceIdOffline.Droid.Tensorflow
{
    public class TensorflowClassifier : IClassifier
    {
        /** Name of the model file stored in Assets. */
        private static string MODEL_PATH = "model.tflite";

        /** Name of the label file stored in Assets. */
        private static string LABEL_PATH = "labels.txt";

        /** Dimensions of inputs. */
        private static int DIM_BATCH_SIZE = 1;

        private static int DIM_PIXEL_SIZE = 3;

        static int DIM_IMG_SIZE_X = 224;
        static int DIM_IMG_SIZE_Y = 224;

        private static int IMAGE_MEAN = 128;
        private static float IMAGE_STD = 128.0f;

        public event EventHandler<ClassificationEventArgs> ClassificationCompleted;

        public async Task Classify(byte[] bytes)
        {
            var mappedByteBuffer = GetModelAsMappedByteBuffer();

            var interpreter = new Xamarin.TensorFlow.Lite.Interpreter(mappedByteBuffer);
            var tensor = interpreter.GetInputTensor(0);
            var x = tensor.NumDimensions();
            var shape = tensor.Shape();

            var width = shape[1];
            var height = shape[2];

            var byteBuffer = GetPhotoAsByteBuffer(bytes);

            var sr = new StreamReader(Android.App.Application.Context.Assets.Open(LABEL_PATH));
            var labels = sr.ReadToEnd().Split('\n').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

            var outputLocations = new byte[1][] { new byte[labels.Count] };
            var outputs = Java.Lang.Object.FromArray(outputLocations);          
            interpreter.Run(byteBuffer, outputs);

            var classificationResult = outputs.ToArray<byte[]>();

            var result = new List<Classification>();

            for (var i = 0; i < labels.Count; i++)
            {   
                var label = labels[i];
                result.Add(new Classification(label, classificationResult[0][i]));
            }

            ClassificationCompleted?.Invoke(this, new ClassificationEventArgs(result));
        }

        private MappedByteBuffer GetModelAsMappedByteBuffer()
        {
            var assetDescriptor = Android.App.Application.Context.Assets.OpenFd(MODEL_PATH);
            var inputStream = new FileInputStream(assetDescriptor.FileDescriptor);
            var mappedByteBuffer = inputStream.Channel.Map(FileChannel.MapMode.ReadOnly, assetDescriptor.StartOffset, assetDescriptor.DeclaredLength);

            return mappedByteBuffer;
        }
        private ByteBuffer GetPhotoAsByteBuffer(byte[] image)
        {
            var bitmap = BitmapFactory.DecodeByteArray(image, 0, image.Length);
            var resizedBitmap = Bitmap.CreateScaledBitmap(bitmap, DIM_IMG_SIZE_X, DIM_IMG_SIZE_Y, true);

            var modelInputSize = DIM_BATCH_SIZE * DIM_IMG_SIZE_X * DIM_IMG_SIZE_Y * DIM_PIXEL_SIZE /** 4*/;
            var byteBuffer = ByteBuffer.AllocateDirect(modelInputSize);
            byteBuffer.Order(ByteOrder.NativeOrder());

            var pixels = new int[DIM_IMG_SIZE_X * DIM_IMG_SIZE_Y];
            resizedBitmap.GetPixels(pixels, 0, resizedBitmap.Width, 0, 0, resizedBitmap.Width, resizedBitmap.Height);

            var pixel = 0;

            for (var i = 0; i < DIM_IMG_SIZE_X; i++)
            {
                for (var j = 0; j < DIM_IMG_SIZE_Y; j++)
                {
                    var val = pixels[pixel++];

                    byteBuffer.Put((sbyte)((val >> 16) & 0xFF));
                    byteBuffer.Put((sbyte)((val >> 8) & 0xFF));
                    byteBuffer.Put((sbyte)(val & 0xFF));

                    //byteBuffer.Put((sbyte)((((val >> 16) & 0xFF) - IMAGE_MEAN) / IMAGE_STD));
                    //byteBuffer.Put((sbyte)((((val >> 8) & 0xFF) - IMAGE_MEAN) / IMAGE_STD));
                    //byteBuffer.Put((sbyte)((((val) & 0xFF) - IMAGE_MEAN) / IMAGE_STD));

                    //byteBuffer.PutFloat((((val >> 16) & 0xFF) ));
                    //byteBuffer.PutFloat((((val >> 8) & 0xFF) ));
                    //byteBuffer.PutFloat((((val) & 0xFF) ));

                    //byteBuffer.PutFloat((((val >> 16) & 0xFF) - IMAGE_MEAN) / IMAGE_STD);
                    //byteBuffer.PutFloat((((val >> 8) & 0xFF) - IMAGE_MEAN) / IMAGE_STD);
                    //byteBuffer.PutFloat((((val) & 0xFF) - IMAGE_MEAN) / IMAGE_STD);
                }
            }

            bitmap.Recycle();

            return byteBuffer;
        }
    }
}