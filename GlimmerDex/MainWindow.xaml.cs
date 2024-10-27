using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace GlimmerDex
{
    public partial class MainWindow : Window
    {
        private const string ApiBaseUrl = "https://pokeapi.co/api/v2/pokemon/";
        private const string SaveFilePath = "pokemon_data.json";
        private List<PokemonData> allPokemonList;
        private PokemonData currentPokemonData;
        private Dictionary<string, PokemonData> savedData = new Dictionary<string, PokemonData>();

        public MainWindow()
        {
            InitializeComponent();
            LoadAllPokemonData();
            LoadEncounterData();
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveEncounterData();
        }

        private async void LoadAllPokemonData()
        {
            allPokemonList = new List<PokemonData>();
            List<int> shinyLockedIds = new List<int> { 385, 494, 647, 720, 721, 789, 790, 801, 802, 808, 809, 891,
            892, 893, 896, 897, 898, 905, 1001, 1002, 1003, 1007, 1008, 1009, 1010, 1014, 1015, 1016, 1017, 1020,
            1021, 1022, 1023, 1024, 1025};
            for (int id = 1; id <= 1025; id++)
            {
                var pokemon = await GetPokemonAsync(id);
                pokemon.IsShinyLocked = shinyLockedIds.Contains(id);
                allPokemonList.Add(pokemon);
            }
        }

        private void PokemonSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchQuery = pokemonSearchBox.Text.ToLower();
            List<PokemonData> filteredList = allPokemonList.FindAll(p => p.Name.ToLower().Contains(searchQuery));

            pokemonListBox.Items.Clear();
            foreach (var pokemon in filteredList)
            {
                pokemonListBox.Items.Add(pokemon);
            }
        }

        private async Task<PokemonData> GetPokemonAsync(int id)
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetStringAsync(ApiBaseUrl + id);
                var pokemon = JsonConvert.DeserializeObject<PokemonData>(response);
                pokemon.Icon = $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/{id}.png";
                return pokemon;
            }
        }

        private void PokemonListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pokemonListBox.SelectedItem != null)
            {
                currentPokemonData = (PokemonData)pokemonListBox.SelectedItem;
                currentPokemonLabel.Content = $"Currently hunting: {currentPokemonData.Name}";

                var bitmap = new BitmapImage(new Uri(currentPokemonData.Icon));
                selectedPokemonIcon.Source = bitmap;

                // Check if the Pokémon is shiny-locked and update button accordingly
                if (currentPokemonData.IsShinyLocked)
                {
                    shinyButton.IsEnabled = false;
                }
                else
                {
                    shinyButton.IsEnabled = true;
                }

                encounterCountLabel.Content = "Encounters: 0";
                eggsCountLabel.Content = "Eggs Hatched: 0";
                sosCountLabel.Content = "SOS Encounters: 0";
                outbreakCountLabel.Content = "Outbreaks: 0";
                catchComboCountLabel.Content = "Catch Combo: 0";
                softresetsCountLabel.Content = "Soft Resets: 0";
                dexNavCountLabel.Content = "DexNav Encounters: 0";
                shinyButton.Content = "Mark as Shiny";
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
                    shinyButton.Content = currentPokemonData.IsShiny ? "Unmark Shiny" : "Mark as Shiny";
                    selectedPokemonIcon.Opacity = currentPokemonData.IsShiny ? 0.5 : 1.0;
                }
                else
                {
                    // Reset if no previous data
                    encounterCountLabel.Content = "Encounters: 0";
                    shinyButton.Content = "Mark as Shiny";
                    selectedPokemonIcon.Opacity = 1.0;
                }
            }
        }

        private void AddEncounterButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPokemonData != null)
            {
                currentPokemonData.Encounters++;
                encounterCountLabel.Content = $"Encounters: {currentPokemonData.Encounters}";
                SaveEncounterData();
            }
        }

        private void AddEggButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPokemonData != null)
            {
                currentPokemonData.EggsHatched++;
                eggsCountLabel.Content = $"Eggs Hatched: {currentPokemonData.EggsHatched}";
                SaveEncounterData();
            }
        }
        private void AddSoftResetsButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPokemonData == null) return;

            currentPokemonData.SoftResets++;
            softresetsCountLabel.Content = $"Soft Resets: {currentPokemonData.SoftResets}";
            SaveEncounterData();
        }

        // Button click event to add an SOS encounter
        private void AddSOSButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPokemonData == null) return;

            currentPokemonData.SOSEncounters++;
            sosCountLabel.Content = $"SOS Chaining: {currentPokemonData.SOSEncounters}";
            SaveEncounterData();
        }

        // Button click event to add a Catch Combo encounter
        private void AddCatchComboButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPokemonData == null) return;

            currentPokemonData.CatchComboEncounters++;
            catchComboCountLabel.Content = $"Catch Combo: {currentPokemonData.CatchComboEncounters}";
            SaveEncounterData();
        }

        // Button click event to add an Outbreak encounter
        private void AddOutbreakButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPokemonData == null) return;

            currentPokemonData.OutbreakEncounters++;
            outbreakCountLabel.Content = $"Outbreaks: {currentPokemonData.OutbreakEncounters}";
            SaveEncounterData();
        }

        private void AddDexNavButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPokemonData == null) return;

            currentPokemonData.DexNavEncounters++;
            dexNavCountLabel.Content = $"DexNav Encounters: {currentPokemonData.DexNavEncounters}";
            SaveEncounterData();
        }

        private void ShinyButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPokemonData != null)
            {
                if (currentPokemonData.IsShinyLocked)
                {
                    shinyButton.IsEnabled = false;
                    shinyButton.Content = "Shiny Locked";
                }
                else
                {
                    currentPokemonData.IsShiny = !currentPokemonData.IsShiny;
                    shinyButton.Content = currentPokemonData.IsShiny ? "Unmark Shiny" : "Mark as Shiny";
                    selectedPokemonIcon.Opacity = currentPokemonData.IsShiny ? 0.5 : 1.0;
                    SaveEncounterData();
                }
            }
        }

        private void LoadEncounterData()
        {
            if (File.Exists(SaveFilePath))
            {
                var jsonData = File.ReadAllText(SaveFilePath);
                savedData = JsonConvert.DeserializeObject<Dictionary<string, PokemonData>>(jsonData) ?? new Dictionary<string, PokemonData>();
            }
        }

        private void SaveEncounterData()
        {
            if (currentPokemonData != null)
            {
                savedData[currentPokemonData.Name] = currentPokemonData;
            }
            File.WriteAllText(SaveFilePath, JsonConvert.SerializeObject(savedData, Formatting.Indented));
        }
    }

    public class PokemonData
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public int Encounters { get; set; }
        public int EggsHatched { get; set; }
        public int SoftResets { get; set; }
        public int SOSEncounters { get; set; }
        public int CatchComboEncounters { get; set; }
        public int OutbreakEncounters { get; set; }
        public int DexNavEncounters { get; set; }
        public bool IsShiny { get; set; }
        public bool IsShinyLocked { get; set; }
    }
}
