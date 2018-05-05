﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CB_5e.Helpers;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using CB_5e.ViewModels;
using OGL.Features;
using OGL.Descriptions;
using OGL;
using CB_5e.ViewModels.Modify;
using CB_5e.ViewModels.Modify.Collections;
using CB_5e.Services;
using OGL.Monsters;
using System.IO;
using System.Xml.Serialization;

namespace CB_5e.Views.Modify.Collections
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MonsterTraitListPage : ContentPage
	{
        public static XmlSerializer Serializer = new XmlSerializer(typeof(MonsterTrait));
        public static XmlSerializer SerializerAction = new XmlSerializer(typeof(MonsterAction));
        private List<MonsterTrait> entries;
        public ObservableRangeCollection<MonsterTraitViewModel> Entries { get; set; } = new ObservableRangeCollection<MonsterTraitViewModel>();
        public IEditModel Model { get; private set; }
        public string Property { get; private set; }
        private int move = -1;
        private bool Modal = true;
        public Command Undo { get => Model.Undo; }
        public Command Redo { get => Model.Redo; }
        public bool Actions { get; set; }

        public MonsterTraitListPage(IEditModel parent, string property, bool actions = true, bool modal = true)
        {
            Actions = actions;
            Model = parent;
            Modal = modal;
            parent.PropertyChanged += Parent_PropertyChanged;
            Property = property;
            UpdateEntries();
			InitializeComponent ();
            InitToolbar(Modal);
            BindingContext = this;
        }

        private void InitToolbar(bool modal)
        {
            if (Modal)
            {
                ToolbarItem undo = new ToolbarItem() { Text = "Undo" };
                undo.SetBinding(MenuItem.CommandProperty, new Binding("Undo"));
                ToolbarItems.Add(undo);
                ToolbarItem redo = new ToolbarItem() { Text = "Redo" };
                redo.SetBinding(MenuItem.CommandProperty, new Binding("Redo"));
                ToolbarItems.Add(redo);
            }
            ToolbarItem add = new ToolbarItem() { Text = "Add" };
            add.Clicked += Add_Clicked;
            ToolbarItems.Add(add);
            if (Actions)
            {
                ToolbarItem adda = new ToolbarItem() { Text = "Add Action" };
                adda.Clicked += AddAction_Clicked;
                ToolbarItems.Add(adda);
            }
            ToolbarItem paste = new ToolbarItem() { Text = "Paste" };
            paste.Clicked += Paste_Clicked;
            ToolbarItems.Add(paste);
        }

        private void Parent_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "" || e.PropertyName == null || e.PropertyName == Property) UpdateEntries();
        }

        private void UpdateEntries()
        {
            entries = (List<MonsterTrait>)Model.GetType().GetRuntimeProperty(Property).GetValue(Model);
            Fill();
        }
        private void Fill() => Entries.ReplaceRange(entries.Select(f => new MonsterTraitViewModel(f)));

        private void Paste_Clicked(object sender, EventArgs e)
        {
            try
            {
                Model.MakeHistory();
                using (StringReader sr = new StringReader(DependencyService.Get<IClipboardService>().GetTextData()))
                {
                    object o = SerializerAction.Deserialize(sr);
                    if (o is MonsterAction msb)
                    {
                        if (Actions) entries.Add(msb);
                        else entries.Add(new MonsterTrait(msb.Name, msb.Text));
                    }
                    Fill();
                }
            } catch (Exception)
            {
                try
                {
                    using (StringReader sr = new StringReader(DependencyService.Get<IClipboardService>().GetTextData()))
                    {
                        object o = Serializer.Deserialize(sr);
                        if (o is MonsterTrait msb) entries.Add(msb);
                        Fill();
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        private async void Add_Clicked(object sender, EventArgs e)
        {
            Model.MakeHistory();
            MonsterTrait t = new MonsterTrait();
            MonsterTraitViewModel vm = new MonsterTraitViewModel(t);
            entries.Add(t);
            Entries.Add(vm);
            await Navigation.PushAsync(new MonsterTraitEditPage(vm));
        }

        private async void AddAction_Clicked(object sender, EventArgs e)
        {
            Model.MakeHistory();
            MonsterAction t = new MonsterAction();
            MonsterTraitViewModel vm = new MonsterTraitViewModel(t);
            entries.Add(t);
            Entries.Add(vm);
            await Navigation.PushAsync(new MonsterActionEditPage(vm));
        }

        private async void Entries_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is MonsterTraitViewModel fvm) {
                if (move >= 0)
                {
                    Model.MakeHistory();
                    foreach (MonsterTraitViewModel ff in Entries) ff.Moving = false;
                    int target = entries.FindIndex(ff => fvm.Entry == ff);
                    if (target >= 0 && move != target)
                    {
                        entries.Insert(target, entries[move]);
                        if (target < move) move++;
                        entries.RemoveAt(move);
                        Fill();
                        (sender as ListView).SelectedItem = null;
                    }
                    move = -1;
                } else
                {
                    Model.MakeHistory();
                    if (fvm.Entry is MonsterAction) await Navigation.PushAsync(new MonsterActionEditPage(fvm));
                    else await Navigation.PushAsync(new MonsterTraitEditPage(fvm));
                    (sender as ListView).SelectedItem = null;
                }
            }
        }

        private void Delete_Clicked(object sender, EventArgs e)
        {
            if (((MenuItem)sender).BindingContext is MonsterTraitViewModel f)
            {
                Model.MakeHistory();
                int i = entries.FindIndex(ff => f.Entry == ff);
                entries.RemoveAt(i);
                Fill();
            }
        }

        private async void Info_Clicked(object sender, EventArgs e)
        {
            if ((sender as MenuItem).BindingContext is MonsterTraitViewModel f) await Navigation.PushAsync(InfoPage.Show(new Feature(f.Text, f.Detail)));
        }

        private void Move_Clicked(object sender, EventArgs e)
        {
            if (((MenuItem)sender).BindingContext is MonsterTraitViewModel f)
            {
                foreach (MonsterTraitViewModel ff in Entries) ff.Moving = false;
                f.Moving = true;
                move = entries.FindIndex(ff => f.Entry == ff);
            }
            
        }

        private void Cut_Clicked(object sender, EventArgs e)
        {
            if (((MenuItem)sender).BindingContext is MonsterTraitViewModel f)
            {
                Model.MakeHistory();
                using (StringWriter sw = new StringWriter())
                {
                    if (f.Entry is MonsterAction ma) SerializerAction.Serialize(sw, ma);
                    else Serializer.Serialize(sw, f.Entry);
                    DependencyService.Get<IClipboardService>().PutTextData(sw.ToString(), f.Detail);
                }
                int i = entries.FindIndex(ff => f.Entry == ff);
                entries.RemoveAt(i);
                Fill();
            }
        }

        private void Copy_Clicked(object sender, EventArgs e)
        {
            if (((MenuItem)sender).BindingContext is MonsterTraitViewModel f)
            {
                using (StringWriter sw = new StringWriter())
                {
                    if (f.Entry is MonsterAction ma) SerializerAction.Serialize(sw, ma);
                    else Serializer.Serialize(sw, f.Entry);
                    DependencyService.Get<IClipboardService>().PutTextData(sw.ToString(), f.Detail);
                }
            }
        }

        protected override bool OnBackButtonPressed()
        {
            if (Modal)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    if (Model.UnsavedChanges > 0)
                    {
                        if (await DisplayAlert("Unsaved Changes", "You have " + Model.UnsavedChanges + " unsaved changes. Do you want to save them before leaving?", "Yes", "No"))
                        {
                            bool written = await Model.SaveAsync(false);
                            if (!written)
                            {
                                if (await DisplayAlert("File Exists", "Overwrite File?", "Yes", "No"))
                                {
                                    await Model.SaveAsync(true);
                                    await Navigation.PopModalAsync();
                                }
                            }
                            else await Navigation.PopModalAsync();
                        }
                        else await Navigation.PopModalAsync();
                    }
                    else await Navigation.PopModalAsync();
                });
            }
            return Modal;
        }
    }
}