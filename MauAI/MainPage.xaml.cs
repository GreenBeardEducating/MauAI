using System.Collections.ObjectModel;
using Plugin.Maui.Audio;

namespace MauAI;

public partial class MainPage : ContentPage
{
    private readonly AiService _aiService = new();
    private readonly IAudioManager _audioManager;
    private IAudioRecorder? _recorder;

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public MainPage()
    {
        InitializeComponent();

        _audioManager = AudioManager.Current;
        BindingContext = this;
    }

    private async void OnMicrophoneClicked(object? sender, EventArgs e)
    {
        if (_recorder?.IsRecording == true)
        {
            await StopRecordingAsync();
            return;
        }

        var permission = await Permissions.RequestAsync<Permissions.Microphone>();

        if (permission != PermissionStatus.Granted)
        {
            await DisplayAlert(
                "Microphone permission",
                "Microphone access is required for voice input.",
                "OK");

            return;
        }

        try
        {
            _recorder = _audioManager.CreateRecorder();
            await _recorder.StartAsync();

            MicrophoneButton.Text = "⏹";
            MicrophoneButton.BackgroundColor = Colors.IndianRed;
            MicrophoneButton.TextColor = Colors.White;
        }
        catch (Exception exception)
        {
            await DisplayAlert("Recording error", exception.Message, "OK");
        }
    }

    private async Task StopRecordingAsync()
    {
        var recorder = _recorder;

        if (recorder is null)
        {
            return;
        }

        MicrophoneButton.IsEnabled = false;

        try
        {
            var audioSource = await recorder.StopAsync();

            await using var audioStream = audioSource.GetAudioStream();

            var prompt = await _aiService.TranscribeAsync(
                audioStream,
                "voice.m4a");

            if (!string.IsNullOrWhiteSpace(prompt))
            {
                PromptEditor.Text = prompt;
                await SendPromptAsync(prompt);
            }
        }
        catch (Exception exception)
        {
            await DisplayAlert("Speech recognition error", exception.Message, "OK");
        }
        finally
        {
            _recorder = null;
            MicrophoneButton.Text = "🎤";
            MicrophoneButton.BackgroundColor = Color.FromArgb("#E9E3FF");
            MicrophoneButton.TextColor = Color.FromArgb("#512BD4");
            MicrophoneButton.IsEnabled = true;
        }
    }

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        await SendPromptAsync(PromptEditor.Text);
    }

    private async Task SendPromptAsync(string? value)
    {
        var prompt = value?.Trim();

        if (string.IsNullOrWhiteSpace(prompt))
        {
            return;
        }

        PromptEditor.Text = string.Empty;
        SendButton.IsEnabled = false;
        MicrophoneButton.IsEnabled = false;

        Messages.Add(new ChatMessage("You", prompt));
        await ScrollToLatestMessageAsync();

        try
        {
            var response = await _aiService.AskAi(prompt);

            Messages.Add(new ChatMessage(
                "AI",
                string.IsNullOrWhiteSpace(response)
                    ? "The AI returned an empty response."
                    : response));
        }
        catch (HttpRequestException exception)
        {
            Messages.Add(new ChatMessage("AI", exception.Message));
        }
        catch (Exception exception)
        {
            Messages.Add(new ChatMessage("AI", $"Unexpected error: {exception.Message}"));
        }
        finally
        {
            SendButton.IsEnabled = true;
            MicrophoneButton.IsEnabled = true;

            await ScrollToLatestMessageAsync();
            PromptEditor.Focus();
        }
    }

    private async Task ScrollToLatestMessageAsync()
    {
        await Task.Delay(50);

        if (Messages.Count > 0)
        {
            MessagesView.ScrollTo(
                Messages[^1],
                position: ScrollToPosition.End,
                animate: true);
        }
    }
}
