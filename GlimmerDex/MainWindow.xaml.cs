using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace GlimmerDex
{
    public partial class MainWindow : Window
    {
        private const string ApiBaseUrl = "https://pokeapi.co/api/v2/pokemon/";
        private const string SaveFilePath = "pokemon_data.json";
        public ObservableCollection<PokemonData> DisplayedPokemonList { get; set; }
        private List<PokemonData> allPokemonList;
        private PokemonData currentPokemonData;
        private Dictionary<string, PokemonData> savedData = new Dictionary<string, PokemonData>();
        private int currentIndex = 0;
        private const int PageSize = 100;
        private int totalPokemons = 1025;
        private int globalTotalShinies = 0;
        private List<HistoryEntry> historyEntries = new List<HistoryEntry>();

        public MainWindow()
        {
            InitializeComponent();
            DisplayedPokemonList = new ObservableCollection<PokemonData>();
            allPokemonList = new List<PokemonData>();
            currentPokemonData = new PokemonData();
            DataContext = this;
            LoadAllPokemonDataAsync();
            LoadEncounterData();
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveEncounterData();
        }

        private async void LoadAllPokemonDataAsync()
        {
            try
            {
                //Current shiny locked Pokémon IDs. Will update if things change in the future.
                var shinyLockedIds = new HashSet<int> { 385, 494, 647, 720, 721, 789, 790, 801, 802, 808, 809, 891, 892, 893, 896, 897, 898, 905, 1001, 1002, 1003, 1007, 1008, 1009, 1010, 1014, 1015, 1016, 1017, 1020, 1021, 1022, 1023, 1024, 1025 };

                int batchSize = 50; // Number of Pokémon to fetch in each batch
                for (int i = 1; i <= totalPokemons; i += batchSize)
                {
                    var tasks = new List<Task<PokemonData>>();
                    for (int id = i; id < i + batchSize && id <= totalPokemons; id++)
                    {
                        tasks.Add(GetPokemonAsync(id, shinyLockedIds.Contains(id)));        // Check if the Pokémon is shiny-locked
                    }

                    var pokemonDataList = await Task.WhenAll(tasks);
                    allPokemonList.AddRange(pokemonDataList);

                    UpdatePokemonListBox();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while loading Pokémon data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // Scroller to load more Pokemon, once user scrolls to the bottom of the list 
        private void UpdatePokemonListBox()
        {
            var pagedPokemonList = allPokemonList.Skip(currentIndex).Take(PageSize).ToList();
            currentIndex += PageSize;

            foreach (var pokemon in pagedPokemonList)
            {
                DisplayedPokemonList.Add(pokemon);
            }
        }

        // Fetch Pokemon data from the API
        private async Task<PokemonData> GetPokemonAsync(int id, bool isShinyLocked)
        {
            int maxRetries = 3;
            int delay = 2000; // 2 seconds delay between retries

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using (HttpClientHandler handler = new HttpClientHandler())
                    {
                        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                        using (HttpClient client = new HttpClient(handler))
                        {
                            client.Timeout = TimeSpan.FromSeconds(30); // Set timeout to 30 seconds
                            var response = await client.GetStringAsync(ApiBaseUrl + id);
                            var pokemon = JsonConvert.DeserializeObject<PokemonData>(response);
                            pokemon.Icon = $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/{id}.png";
                            pokemon.ShinyIcon = $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/shiny/{id}.png";
                            pokemon.IsShinyLocked = isShinyLocked;
                            return pokemon;
                        }
                    }
                }
                catch (TaskCanceledException ex) when (ex.CancellationToken == CancellationToken.None)
                {
                    if (attempt == maxRetries)
                    {
                        throw new Exception($"Request timed out for Pokémon ID {id}: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries)
                    {
                        throw new Exception($"Failed to fetch data for Pokémon ID {id}: {ex.Message}");
                    }
                }

                await Task.Delay(delay);
            }

            throw new Exception($"Failed to fetch data for Pokémon ID {id} after {maxRetries} attempts.");
        }

        // Filter Pokemon list based on search query
        private void PokemonSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchQuery = pokemonSearchBox.Text.ToLower();
            var filteredList = allPokemonList.Where(p => p.Name.ToLower().Contains(searchQuery)).ToList(); // Filter Pokemon by name

            DisplayedPokemonList.Clear(); // Clear the current list
            foreach (var pokemon in filteredList)
            {
                DisplayedPokemonList.Add(pokemon);
            }
        }

        private void PokemonItem_Click(object sender, MouseButtonEventArgs e)
        {
            // Check if the clicked item is a Pokemon
            if (((FrameworkElement)sender).DataContext is PokemonData selectedPokemon)
            {
                currentPokemonData = selectedPokemon;
                currentPokemonLabel.Content = $"Currently hunting: {currentPokemonData.Name}";

                var bitmap = new BitmapImage(new Uri(currentPokemonData.Icon));
                selectedPokemonIcon.Source = bitmap;

                // Check if the Pokemon is shiny-locked and update button accordingly
                shinyButton.IsEnabled = !currentPokemonData.IsShinyLocked;

                // Enable encounter and method buttons
                addEncounterButton.IsEnabled = true;
                addEggButton.IsEnabled = true;
                addSoftResetsButton.IsEnabled = true;
                addSOSButton.IsEnabled = true;
                addCatchComboButton.IsEnabled = true;
                addOutbreakButton.IsEnabled = true;
                addDexNavButton.IsEnabled = true;

                // Reset encounter and method counts
                encounterCountLabel.Content = "Encounters: 0";
                eggsCountLabel.Content = "Eggs Hatched: 0";
                sosCountLabel.Content = "SOS Encounters: 0";
                outbreakCountLabel.Content = "Outbreaks: 0";
                catchComboCountLabel.Content = "Catch Combo: 0";
                softresetsCountLabel.Content = "Soft Resets: 0";
                dexNavCountLabel.Content = "DexNav Encounters: 0";
                shinyButton.Content = $"Add Shiny ({currentPokemonData.TotalShinies})";
                selectedPokemonIcon.Opacity = 1.0;

                // Load previous encounter and shiny data if it exists
                if (savedData.ContainsKey(currentPokemonData.Name))
                {
                    currentPokemonData = savedData[currentPokemonData.Name];
                    eggsCountLabel.Content = $"Eggs Hatched: {currentPokemonData.EggsHatched}";
                    encounterCountLabel.Content = $"Encounters: {currentPokemonData.Encounters}";
                    softresetsCountLabel.Content = $"Soft Resets: {currentPokemonData.SoftResets}";
                    sosCountLabel.Content = $"SOS Chaining: {currentPokemonData.SOSEncounters}";
                    catchComboCountLabel.Content = $"Catch Combo: {currentPokemonData.CatchComboEncounters}";
                    outbreakCountLabel.Content = $"Outbreaks: {currentPokemonData.OutbreakEncounters}";
                    dexNavCountLabel.Content = $"DexNav Encounters: {currentPokemonData.DexNavEncounters}";
                    shinyButton.Content = $"Add Shiny ({currentPokemonData.TotalShinies})";

                    selectedPokemonIcon.Opacity = currentPokemonData.IsShiny ? 0.5 : 1.0;
                }
            }
        }
        //self explanatory, adds encounters to the current Pokemon
        private void AddEncounterButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPokemonData != null)
            {
                currentPokemonData.Encounters++;
                encounterCountLabel.Content = $"Encounters: {currentPokemonData.Encounters}";
                SaveEncounterData();
            }
        }
        // Adds eggs hatched to the current Pokemon
        private void AddEggButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPokemonData != null)
            {
                currentPokemonData.EggsHatched++;
                eggsCountLabel.Content = $"Eggs Hatched: {currentPokemonData.EggsHatched}";
                SaveEncounterData();
            }
        }

        // Adds soft reset encounters to the current Pokemon
        private void AddSoftResetsButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPokemonData == null) return;

            currentPokemonData.SoftResets++;
            softresetsCountLabel.Content = $"Soft Resets: {currentPokemonData.SoftResets}";
            SaveEncounterData();
        }

        // Adds SOS encounters to the current Pokemon
        private void AddSOSButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPokemonData == null) return;

            currentPokemonData.SOSEncounters++;
            sosCountLabel.Content = $"SOS Chaining: {currentPokemonData.SOSEncounters}";
            SaveEncounterData();
        }

        // Adds catch combo encounters to the current Pokemon
        private void AddCatchComboButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPokemonData == null) return;

            currentPokemonData.CatchComboEncounters++;
            catchComboCountLabel.Content = $"Catch Combo: {currentPokemonData.CatchComboEncounters}";
            SaveEncounterData();
        }

        //  Adds outbreak encounters to the current Pokemon
        private void AddOutbreakButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPokemonData == null) return;

            currentPokemonData.OutbreakEncounters++;
            outbreakCountLabel.Content = $"Outbreaks: {currentPokemonData.OutbreakEncounters}";
            SaveEncounterData();
        }

        // Adds DexNav encounters to the current Pokemon
        private void AddDexNavButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPokemonData == null) return;

            currentPokemonData.DexNavEncounters++;
            dexNavCountLabel.Content = $"DexNav Encounters: {currentPokemonData.DexNavEncounters}";
            SaveEncounterData();
        }

        // Marks Pokemon as shiny
        private void ShinyButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPokemonData == null)
            {
                MessageBox.Show("Please select a Pokémon first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); // checks if a Pokemon is selected
                return;
            }

            if (currentPokemonData.IsShinyLocked)
            {
                shinyButton.IsEnabled = false;
                shinyButton.Content = "Shiny Locked"; 
                return;
            }

            // Check if the Pokémon has been encountered or hatched
            if (currentPokemonData.Encounters == 0 && currentPokemonData.EggsHatched == 0 && currentPokemonData.SoftResets == 0 &&
                currentPokemonData.SOSEncounters == 0 && currentPokemonData.CatchComboEncounters == 0 && currentPokemonData.OutbreakEncounters == 0 &&
                currentPokemonData.DexNavEncounters == 0)
            {
                MessageBox.Show("You must have at least one encounter to add a shiny.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedGame = (ComboBoxItem)gameSelector.SelectedItem;
            if (selectedGame == null)
            {
                MessageBox.Show("Please select a game first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); // checks if a game is selected
                return;
            }

          
            string gameName = selectedGame.Content.ToString();
            if (!currentPokemonData.GamesCaught.Contains(gameName))
            {
                currentPokemonData.GamesCaught.Add(gameName);
            } // adds the game to the list of games caught

            if (!currentPokemonData.GameEncounters.ContainsKey(gameName))
            {
                currentPokemonData.GameEncounters[gameName] = new EncounterData(); // Initialize encounter data for the game
            }

            // Update encounter data for the game
            var encounterData = currentPokemonData.GameEncounters[gameName];
            encounterData.Encounters = currentPokemonData.Encounters;
            encounterData.EggsHatched = currentPokemonData.EggsHatched;
            encounterData.SoftResets = currentPokemonData.SoftResets;
            encounterData.SOSEncounters = currentPokemonData.SOSEncounters;
            encounterData.CatchComboEncounters = currentPokemonData.CatchComboEncounters;
            encounterData.OutbreakEncounters = currentPokemonData.OutbreakEncounters;
            encounterData.DexNavEncounters = currentPokemonData.DexNavEncounters;

            // Reset encounter counts
            currentPokemonData.Encounters = 0;
            currentPokemonData.EggsHatched = 0;
            currentPokemonData.SoftResets = 0;
            currentPokemonData.SOSEncounters = 0;
            currentPokemonData.CatchComboEncounters = 0;
            currentPokemonData.OutbreakEncounters = 0;
            currentPokemonData.DexNavEncounters = 0;

            // Update encounter labels
            encounterCountLabel.Content = "Encounters: 0";
            eggsCountLabel.Content = "Eggs Hatched: 0";
            softresetsCountLabel.Content = "Soft Resets: 0";
            sosCountLabel.Content = "SOS Encounters: 0";
            catchComboCountLabel.Content = "Catch Combo: 0";
            outbreakCountLabel.Content = "Outbreaks: 0";
            dexNavCountLabel.Content = "DexNav Encounters: 0";

            currentPokemonData.TotalShinies++;
            globalTotalShinies++;
            shinyButton.Content = $"Add Shiny ({currentPokemonData.TotalShinies})";

            // Add to history
            historyEntries.Add(new HistoryEntry
            {
                PokemonName = currentPokemonData.Name,
                Timestamp = DateTime.Now
            });

            totalShiniesLabel.Content = $"Total Shinies: {globalTotalShinies}";
            SaveEncounterData();
            UpdateShinyPokemonList();
        }

        // Update the shiny Pokemon list
        private void UpdateShinyPokemonList()
        {
            var shinyPokemonList = savedData.Values.Where(p => p.TotalShinies > 0).ToList(); // Filter shiny Pokemon
            DisplayedPokemonList.Clear();
            foreach (var shiny in shinyPokemonList)
            {
                DisplayedPokemonList.Add(shiny);
            }
        }

        private void ShinyPokemon_Click(object sender, MouseButtonEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is PokemonData shinyPokemon)
            {
                ShowShinyPokemonCard(shinyPokemon);
            }
        }

        // Show shiny Pokemon card with details (game encounters, etc.)
        private void ShowShinyPokemonCard(PokemonData shinyPokemon)
        {
            var encounterDetails = shinyPokemon.GameEncounters
                .Select(kvp => $"Game: {kvp.Key}\nEncounters: {kvp.Value.Encounters}\nEggs Hatched: {kvp.Value.EggsHatched}\nSoft Resets: {kvp.Value.SoftResets}\nSOS Encounters: {kvp.Value.SOSEncounters}\nCatch Combo: {kvp.Value.CatchComboEncounters}\nOutbreaks: {kvp.Value.OutbreakEncounters}\nDexNav Encounters: {kvp.Value.DexNavEncounters}")
                .ToList();

            var encounterDetailsString = string.Join("\n\n", encounterDetails);

            MessageBox.Show($"Name: {shinyPokemon.Name}\nTotal Shinies: {shinyPokemon.TotalShinies}\nGames Caught: {string.Join(", ", shinyPokemon.GamesCaught)}\n\n{encounterDetailsString}",
                            "Shiny Pokémon Details", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Load encounter data from the JSON file
        private void LoadEncounterData()
        {
            if (File.Exists(SaveFilePath))
            {
                var jsonData = File.ReadAllText(SaveFilePath);
                savedData = JsonConvert.DeserializeObject<Dictionary<string, PokemonData>>(jsonData) ?? new Dictionary<string, PokemonData>();
                globalTotalShinies = savedData.Values.Sum(p => p.TotalShinies);
                totalShiniesLabel.Content = $"Total Shinies: {globalTotalShinies}";
            }
        }

        // Save encounter data to the JSON file
        private void SaveEncounterData()
        {
            if (currentPokemonData != null)
            {
                savedData[currentPokemonData.Name] = currentPokemonData;
            }
            File.WriteAllText(SaveFilePath, JsonConvert.SerializeObject(savedData, Formatting.Indented));
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Check if the user has scrolled to the bottom of the ScrollViewer
            if (e.VerticalOffset == e.ExtentHeight - e.ViewportHeight)
            {
                UpdatePokemonListBox();
            }
        }
        
        
        private void ShinyPokemonButton_Click(object sender, RoutedEventArgs e)
        {
            homeGrid.Visibility = Visibility.Collapsed;
            shinyGrid.Visibility = Visibility.Visible;
            UpdateShinyPokemonList();
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var historyWindow = new HistoryWindow(historyEntries);
            historyWindow.Show();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            homeGrid.Visibility = Visibility.Visible;
            shinyGrid.Visibility = Visibility.Collapsed;
            UpdatePokemonListBox();
        }
    }

    public class PokemonData // Represents the data for each Pokemon
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ShinyIcon { get; set; } = string.Empty;
        public int Encounters { get; set; }
        public int EggsHatched { get; set; }
        public int SoftResets { get; set; }
        public int SOSEncounters { get; set; }
        public int CatchComboEncounters { get; set; }
        public int OutbreakEncounters { get; set; }
        public int DexNavEncounters { get; set; }
        public bool IsShiny { get; set; }
        public bool IsShinyLocked { get; set; }
        public int TotalShinies { get; set; }
        public List<string> GamesCaught { get; set; } = new List<string>();
        public Dictionary<string, EncounterData> GameEncounters { get; set; } = new Dictionary<string, EncounterData>();
    }

    public class EncounterData // Represents the encounter data for each game and method
    {
        public int Encounters { get; set; }
        public int EggsHatched { get; set; }
        public int SoftResets { get; set; }
        public int SOSEncounters { get; set; }
        public int CatchComboEncounters { get; set; }
        public int OutbreakEncounters { get; set; }
        public int DexNavEncounters { get; set; }
    }
}