﻿using MeetingSpy.Models;
using Microsoft.Graph;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace MeetingSpy
{
    public partial class MeetingSpyPage : ContentPage
    {
        private string clientId = "dd122742-c01f-438f-aaac-451ff37ba079";
        string graphEndpoint = "https://graph.microsoft.com/v1.0/me/events";
        string authority = "https://login.microsoftonline.com/common";
        string resource = "https://graph.microsoft.com/";
        string returnUri = "http://meetingspy.com";

        public IPlatformParameters PlatformParameters { get; set; }

        AuthenticationContext authContext = null;

        public MeetingSpyPage()
        {
            InitializeComponent();

            authContext = new AuthenticationContext(authority);

            
        }

		bool needAdal = true;
		protected override async void OnAppearing()
		{
			base.OnAppearing();

			MockImage.Source = ImageSource.FromResource("MeetingSpy.MainMock.png");


			//MockButton.Clicked += (sender, e) => { this.Navigation.PushAsync(new AttendeeDetailsPage()); };

			MockImage.GestureRecognizers.Add(new TapGestureRecognizer()
			{
				Command = new Command(async () =>
				{

					await this.Navigation.PushAsync(new AttendeeDetailsPage());
				})
			});


			if (!needAdal)
				return;

			needAdal = true;
			IsBusy = true;
			await GetMeeting();
			IsBusy = false;
		}

        async Task GetMeeting()
        {
            AuthenticationResult authResult = await GetADALToken(resource);

            try
            {
                var graphserviceClient = new GraphServiceClient(
                    new DelegateAuthenticationProvider(
                        (requestMessage) =>
                        {
                            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", authResult.AccessToken);

                            return Task.FromResult(0);
                        }));

                var request = graphserviceClient.Me.Events.Request();
                var events = await request.GetAsync();

                this.BindingContext = MeetingViewModel.CreateInstance(events[0]);
            }
            catch (ServiceException)
            {
            }
            catch (Exception ex)
            {
				Debug.WriteLine("Error loading current meeting: " + ex.Message);
            }
        }

        private async Task<AuthenticationResult> GetADALToken(string serviceResourceId)
        {
            AuthenticationResult authResult = null;

            try
            {
				authResult = await authContext.AcquireTokenSilentAsync(serviceResourceId, clientId);
                //authResult = await authContext.AcquireTokenAsync(resource, clientId, new Uri(returnUri), this.PlatformParameters);
            }
			catch (Exception ex)
            {
                //if (ex.ErrorCode.Equals(AdalError.FailedToAcquireTokenSilently))
                {
					authResult = await authContext.AcquireTokenAsync(resource, clientId, new Uri(returnUri), this.PlatformParameters);
				}
            }

            return authResult;
        }

        private async Task DoHttpRequest(AuthenticationResult authResult)
        {
            // use this token to get the list of contacts
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + authResult.AccessToken);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            using (HttpResponseMessage response = await client.GetAsync(new Uri(graphEndpoint)))
            {
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    //this.loginBtn.IsVisible = false;
                    //this.BindingContext = ContactViewModel.CreateInstance(content);
                }
            }
        }

        void AttendeeButton_Click(object sender, EventArgs e)
        {
            try
            {
                this.Navigation.PushAsync(new AttendeeDetailsPage());
            }
            catch (Exception)
            {

            }
        }

    }
}
