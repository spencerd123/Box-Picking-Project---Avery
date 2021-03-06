﻿using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;
using ZXing.Mobile;
using ZXing.Net.Mobile.Forms;

namespace MobileScanApp
{
    /*
     *  @author:    Jess Merolla, Graham Hallman-Taylor
     *  @date:      9/18/2020
     *  @summary:
     *
     *  This class will hold the scanner functionality. Takes in a list
     *  created by our order list, and tells us how many scans we have 
     *  remaining after each successful scan. A successful scan happens 
     *  when the barcode scanned matches the barcode number from the
     *  OrderItem.
     *
     *  @source: https://www.c-sharpcorner.com/blogs/using-zxing-code-128-scanner-in-xamarin-forms2
     *
     *  Last edited: Spencer Dusi 11/3/20
     * 
     */
    public partial class ScanPage : ContentPage
    {
        public String barCodeRead; //Barcode that was scanned
        int qtyScanned = 0; //qty we have already scanned
        int remainingScans; //qty we have left to scan
        Boolean doneScanning = false; //turns true when remaining scans = 0
        bool _isScanning = true; //true before every scan, helps stop multiple scans
        public StackLayout stkMainlayout; //Our ScanPage's layout format
        public OrderItem scannableItem; //holds OrderItem currently being picked
        ZXingScannerPage scanPage; //creating our ZXing scan page
        public IList<OrderItem> OIList; //Creating OrderItemList to change our components of the list
        int tempEnteredAmount = -1; //amount of scans a user chooses on pop up. Set to 0 to activate if() statement.
        bool EnteredQtyBool = false; //tells us if we have entered quantity to scan.

        public ScanPage(OrderItem scannableItem, IList<OrderItem> list)
        {
            InitializeComponent(); //calling content formed in ScanPage.xaml
            OIList = list;
            this.scannableItem = scannableItem; //Taking the OrderItem from OrderListView.
            remainingScans = (int)scannableItem.QtyOrdered;

            itemLabel.Text = "Item to be scanned: " + scannableItem.Name; //sets our label with the name of the item in case of mis tap.

            /**
             *  var "options" allows you to choose what options you want your scanner to
             * allow. Currently using it to AutoRotate. Another option, "TryHarder", 
             * gets or sets a flag which cause a deeper look into the bitmap.This
             * just makes it so the camera focuses on the barcode quicker, though it
             * does add room for misscans. 
             */
            var options = new MobileBarcodeScanningOptions
            {
                AutoRotate = true,
                UseFrontCameraIfAvailable = false,
                TryHarder = false
            };

            Button EnterQtyToScan = new Button
            {
                IsVisible = true,
                BorderColor = Color.DarkGray,
                Text = "Enter Amount",
            };

            /*
             * Overlay sets our top and bottom texts, these texts will change once we scan items.
             */
            var overlay = new ZXingDefaultOverlay
            {
                TopText = "Quantity scanned: " + qtyScanned.ToString() + "\t" + "Quantity remaining: " + scannableItem.QtyOrdered.ToString(),
                BottomText = "Item Scanning: " + scannableItem.Name + "\r" + "Located in Section: " + scannableItem.LocationQOH.Replace(Environment.NewLine, " ") + "\r" + " You can enter amount up to one less than quantity remaining."
            };
            //Removes red line across screen
            var redLine = overlay.Children.First(x => x.BackgroundColor == Color.Red);
            overlay.Children.Remove(redLine);

            //Removes top background color
            var topLine = overlay.Children.First();
            overlay.Children.Remove(topLine);

            //Adding a clear lines to have more space to scan.
            var clearLine = overlay.Children[2];
            overlay.Children.AddVertical(clearLine);

            overlay.Children.AddVertical(clearLine);

            //Removes bottom background color
            var bottomLine = overlay.Children.First();
            overlay.Children.Remove(bottomLine);

            //Setting our elements for our button on the scanner page
            EnterQtyToScan.Scale = 1;
            EnterQtyToScan.HorizontalOptions = LayoutOptions.Center;
            EnterQtyToScan.Margin = 20;
            EnterQtyToScan.BackgroundColor = Color.Gray;

            overlay.Children.AddVertical(EnterQtyToScan); //adding our "Enter Amount" button to our overlay
            //Once button is clicked, create a new scanPage with the options and overlay set above
            btnScan.Clicked += async (a, s) =>
            {
                scanPage = new ZXingScannerPage(options, overlay);

                overlay.FlashButtonClicked += (t, ed) => //Can be removed if target device has no flash capability
                {
                    scanPage.ToggleTorch(); //If flash button is clicked it will toggle on/off.
                };

            EnterQtyToScan.Clicked += async (w, q) =>
            {
                EnteredQtyBool = true; //we pressed the "enter quantity to scan" button
                while (tempEnteredAmount > scannableItem.QtyOrdered || tempEnteredAmount == -1) //while the amount we entered is greater than qtyOrdered or has not been entered yet.
                {
                    //Prompt that will have us enter the quantity we wish to scan at once
                    string EnteredAmountToScan = await DisplayPromptAsync(scannableItem.Name, "How many of this item do you intend to pack?", "accept", "cancel", maxLength: 4, keyboard: Keyboard.Numeric);
                    if (EnteredAmountToScan == null)
                    {
                        EnterQtyToScan.Unfocus(); //Cancel button pressed we unfocus the prompt.
                        EnteredQtyBool = false; 
                        return;
                    }
                    try
                    {
                        //tempEnteredAmount must be a valid number (0-one less than remainingScans) otherwise you will get an Error message.
                        tempEnteredAmount = Int32.Parse(EnteredAmountToScan); //try and set our global var
                        if (tempEnteredAmount > remainingScans) //if it exceeds our qty ordered we must display an error
                        {
                            await DisplayAlert("Error", "Amount entered exceeds quantity ordered.", "OK");
                            tempEnteredAmount = -1;
                        }
                        if (tempEnteredAmount < 0) //if it is negative we must display an error
                        {
                            await DisplayAlert("Error", "Amount must be a positive number.", "OK");
                            tempEnteredAmount = -1;
                        }
                        if (tempEnteredAmount == remainingScans) //if it equals our qty ordered we must display an error
                        {
                            await DisplayAlert("Error", "Amount entered must be at least one less than quantity ordered.", "OK");
                            tempEnteredAmount = -1;
                        }
                    }
                    catch
                    {
                        //If the number is a decimal we display an Error message.
                        await DisplayAlert("Error", "Invalid entry: entry must be a whole number.", "OK");
                    }
                }
                RemainingScans(); //After we leave our while loop we call our method to update our variables.
                overlay.TopText = "Quantity scanned: " + qtyScanned.ToString() + "\r\n\r\n" + "Quantity remaining: " + remainingScans.ToString(); //overlay to represent how many scans remain after we enter our amount.
                tempEnteredAmount = -1; //allows us to reenter our loop if we press the button again.
            };
                //Once we capture a barcode we "BeginInvokeOnMainThread" to check what we scanned
                scanPage.OnScanResult += (result) =>
                {
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        if (_isScanning)
                        {
                            _isScanning = false; //bool to stop _isScanning from allowing us to enter if() statement.
                            scanPage.IsAnalyzing = false; //stops scanning barcodes once this is implemented.
                            barCodeRead = result.Text; //sets barcode scanned in as a string
                            if (barCodeMatcher()) //If the scan matches the barcode from the OrderItem list, display the "Barcode Matches" alert.
                            {
                                RemainingScans(); //tells us how many more scan we have
                                overlay.TopText = "Quantity scanned: " + qtyScanned.ToString() + "\r\n\r\n" + "Quantity remaining: " + remainingScans.ToString(); //overlay to represent how many scans remain.
                                await DisplayAlert("Barcode Matches", result.Text + " , " + " Remaining Scans: " + remainingScans.ToString() + " , " + "QtyScanned: " + qtyScanned.ToString(), "OK");
                                if (doneScanning)
                                {
                                    await Navigation.PopModalAsync(); //Takes us back to the page with the scan button to know we are done.
                                    await DisplayAlert("Finished Scanning: ", scannableItem.Name + " is completed.", "OK"); //Alert to know we are done scanning an item.                                
                                    await Navigation.PopAsync(); //Takes us back to the page where we choose which item we are about to scan.
                                    OIList[OIList.IndexOf(scannableItem)].IsPacked = true; //sets our binding property to true
                                }
                            }
                            else
                            {
                                await DisplayAlert("Scanned Barcode", result.Text + " , " + result.BarcodeFormat + " ," + result.ResultPoints[0].ToString() + " , " + " Barcode does NOT match that of the selected item.", "OK"); //Every barcode scanned that does not match will display as an alert.
                            }
                            scanPage.IsAnalyzing = true; //Allows us to scan again once we "ok" the popup.
                            _isScanning = true; //Allows us to be able to reenter our if() statement.
                        }
                    });
                };
                await Navigation.PushModalAsync(scanPage); //Takes us to the page where we see what the camera is picking up
            };
            //If the wrong item is clicked hit the back button to get back to the list of items to be scanned.
            btnBack.Clicked += async (a, s) =>
            {
                await Navigation.PopAsync(); //Takes us back to the page where we choose which item we are about to scan.
            };
            this.Content = Content; //sets our content from the .xaml
        }

        /**
         * Edited by Spencer Dusi 9-29-20.
         * This Method takes the barcodes and test to see if it matches the one on the sheet.
         * If it matches we set our boolean true allowing the pop up to happen.
         * DONE - Link the barcode numbers from the list.
         * @Return - true if the barcode scanned matches the BarcodeID from OrderItem. false otherwise.
         */
        public Boolean barCodeMatcher()
        {
            if (scannableItem.BarcodeID == barCodeRead)
            {
                return true;
            }
            return false;
        }

        /**
         * !!!!!!!!!!!!Edited by Jess Merolla 11/4/2020
         * Created by Spencer Dusi 10-12-20.
         * This method takes the quantity ordered for the specific item
         * and each time the item is scanned it adds to the counter until
         * it has been scanned the amount of times the quantity desires.
         * @Return - remaining amount of scans left on an item
         */
        public int RemainingScans()
        {
            //changed from int to decimal
            decimal qtyOrdered = scannableItem.QtyOrdered;
            if (doneScanning != true)
            {
                if (EnteredQtyBool) //if we entered a custom amount to scan
                {
                    EnteredQtyBool = false; //Make it false so next scan is not custom unless button is pressed again.
                    qtyScanned += tempEnteredAmount; //setting qtyScanned to add our entered quantity.
                }
                else
                {
                    qtyScanned++;
                }
            }
            if (qtyOrdered >= qtyScanned)
            {
                if (qtyOrdered == qtyScanned)
                {
                    doneScanning = true;
                }
                else
                {
                    doneScanning = false;
                }
            }
            remainingScans = (int)qtyOrdered - qtyScanned;
            return remainingScans;
        }
    }
}
