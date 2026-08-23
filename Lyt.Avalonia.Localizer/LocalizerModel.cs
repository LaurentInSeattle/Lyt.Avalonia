namespace Lyt.Avalonia.Localizer;

public sealed class LocalizerModel : ModelBase, ILocalizer
{
    private readonly Application application;
    private readonly FileManagerModel fileManagerModel;

    private LocalizerConfiguration configuration;

    private string? currentLanguage;
    private ResourceDictionary? currentLanguageResource;

    public LocalizerModel(
        IApplicationBase application, 
        ILogger logger, 
        FileManagerModel fileManagerModel) : base(logger)
    {
        if (application is not Application avaloniaApplication)
        {
            string msg = "No valid application object";
            this.Logger.Error(msg);
            throw new Exception(msg);
        }

        this.application = avaloniaApplication;
        this.fileManagerModel = fileManagerModel;
        this.configuration = new();
    }

    public override Task Initialize() => Task.CompletedTask;

    public string? CurrentLanguage => this.currentLanguage; 

    public Task Configure(LocalizerConfiguration configuration)
    {
        if (configuration.IsLikelyValid)
        {
            this.configuration = configuration;
            this.DetectAvailableLanguages();
        }
        else
        {
            this.Logger.Fatal("Invalid configuration");
        }

        return Task.CompletedTask;
    }

    public bool DetectAvailableLanguages()
    {
        // Returns nothing :(   Possible bug ? 
        // Stupid Avalonia AssetLoader is filtering out all axaml files... 
        string uriString = this.configuration.ResourceFolderUriString();
        var assets = AssetLoader.GetAssets(new Uri(uriString), null).ToList();
        return false;
    }

    public bool SelectLanguage(string targetLanguage)
    {
        if (!this.configuration.Languages.Contains(targetLanguage))
        {
            this.Logger.Error(targetLanguage + "is not a supported language.");
            return false;
        }

        try
        {
            var mergedDictionaries = this.application.Resources.MergedDictionaries.ToList();
            if (mergedDictionaries is null)
            {
                this.Logger.Warning("Failed get the MergedDictionaries");
                return false;
            }

            var translations =
                mergedDictionaries.OfType<ResourceInclude>()
                .FirstOrDefault(x => x.Source?.OriginalString?.Contains(this.configuration.LanguagesSubFolder) ?? false);
            if (translations is not null)
            {
                mergedDictionaries.Remove(translations);
                this.Logger.Info("Removed current language");
            }
            else
            {
                this.Logger.Warning("Failed get any Resource Includes");
            }

            string? oldLanguageKey = this.currentLanguage;

            // ! There is an assembly - or else there are no translations 
            ResourcesUtilities.SetExecutingAssembly(this.configuration.Assembly!);

            string resourcePath = this.configuration.ResourcePathString(); 
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                this.Logger.Warning("Failed to find Resource Path for: " + targetLanguage);
                return false;
            }
            
            ResourcesUtilities.SetResourcesPath(resourcePath);

            string resourceFilePath = this.configuration.ResourceFileEmbeddedPathString(targetLanguage); 
            string xamlString = ResourcesUtilities.LoadEmbeddedTextResource(resourceFilePath, out string? path);
            if (string.IsNullOrEmpty(xamlString))
            {
                this.Logger.Warning("Failed to load Resource for: " + targetLanguage );
                return false; 
            }

            var tuple = AxamlParserWriter.ParseResourceFile(xamlString);
            if (!tuple.Item1)
            {
                this.Logger.Warning("Failed to parse Resource for: " + targetLanguage);
                return false;
            }

            var newLanguage = new ResourceDictionary();
            foreach (var kvp in tuple.Item2) 
            {
                newLanguage.Add( kvp.Key, kvp.Value);
            }

            this.application.Resources.MergedDictionaries.Add(newLanguage);
            this.currentLanguageResource = newLanguage;
            this.currentLanguage = targetLanguage;

            var cultureInfo = new CultureInfo(this.currentLanguage);
            var currentThread = Thread.CurrentThread;
            currentThread.CurrentCulture = cultureInfo;
            currentThread.CurrentUICulture = cultureInfo;

            this.Logger.Info("Added new language: " + targetLanguage);
            new LanguageChangedMessage(oldLanguageKey, this.currentLanguage).Publish();
            return true;
        }
        catch (Exception ex)
        {
            this.Logger.Error("Exception thrown trying to switch language\n" + ex.ToString());
            return false;
        }
    }

    public string Lookup(string localizationKey, bool failSilently = false)
    {
        if (string.IsNullOrWhiteSpace(this.currentLanguage) || this.currentLanguageResource is null)
        {
            this.Logger.Warning("No language loaded");
            return localizationKey;
        }

        if (this.currentLanguageResource.TryGetResource(localizationKey, this.application.ActualThemeVariant, out object? resource))
        {
            if (resource is string localized)
            {
                return localized;
            }
        }

        if (!failSilently)
        {
            this.Logger.Warning("Failed to translate: " + localizationKey + " for language: " + this.currentLanguage);
        }

        return localizationKey;
    }

    public string LookupResource(string localizationKey)
    {
        if (string.IsNullOrWhiteSpace(this.currentLanguage) || this.currentLanguageResource is null)
        {
            this.Logger.Warning("No language loaded");
            return string.Empty;
        }

        try
        {
            string name =
                string.Format(
                    "{0}/{1}/{1}_{2}.txt", this.configuration.LanguagesSubFolder, localizationKey, this.currentLanguage);
            string uriString = string.Format("{0}{1}", this.fileManagerModel.Configuration.AvaresUriString(), name);
            var streamReader = new StreamReader(AssetLoader.Open(new Uri(uriString)));
            string localized =
                this.fileManagerModel.LoadResourceFromStream<string>(FileManagerModel.Kind.Text, streamReader);
            if (string.IsNullOrWhiteSpace(localized))
            {
                throw new Exception("No localized data");
            }

            return localized;
        }
        catch (Exception ex)
        {
            this.Logger.Warning("Failed to translate resource: " + localizationKey + " for language: " + this.currentLanguage);
            this.Logger.Warning("Exception thrown: \n" + ex.ToString());
            return string.Empty;
        }
    }
}
