﻿using Dopamine.Core.Settings;
using System.Collections.Generic;
using System.Linq;

namespace Dopamine.CollectionModule
{
    public class Utils
    {
        public static void GetVisibleSongsColumns(ref bool ratingVisible, ref bool artistVisible, ref bool albumVisible, ref bool genreVisible, ref bool lengthVisible, ref bool albumArtistVisible, ref bool trackNumberVisible, ref bool yearVisible, ref bool bitrateVisible)
        {
            ratingVisible = false;
            artistVisible = false;
            albumVisible = false;
            genreVisible = false;
            lengthVisible = false;
            albumArtistVisible = false;
            trackNumberVisible = false;
            yearVisible = false;
            bitrateVisible = false;

            string visibleColumnsSettings = XmlSettingsClient.Instance.Get<string>("TracksGrid", "VisibleColumns");
            string[] visibleColumns = null;

            if (!string.IsNullOrEmpty(visibleColumnsSettings))
            {
                if (visibleColumnsSettings.Contains(";"))
                {
                    visibleColumns = visibleColumnsSettings.Split(';');
                }
                else
                {
                    visibleColumns = new string[] { visibleColumnsSettings };
                }
            }

            if (visibleColumns != null && visibleColumns.Count() > 0)
            {

                foreach (string visibleColumn in visibleColumns)
                {
                    switch (visibleColumn)
                    {
                        case "rating":
                            ratingVisible = true;
                            break;
                        case "artist":
                            artistVisible = true;
                            break;
                        case "album":
                            albumVisible = true;
                            break;
                        case "genre":
                            genreVisible = true;
                            break;
                        case "length":
                            lengthVisible = true;
                            break;
                        case "albumartist":
                            albumArtistVisible = true;
                            break;
                        case "tracknumber":
                            trackNumberVisible = true;
                            break;
                        case "year":
                            yearVisible = true;
                            break;
                        case "bitrate":
                            bitrateVisible = true;
                            break;
                    }
                }
            }
        }


        public static void SetVisibleSongsColumns(bool ratingVisible, bool artistVisible, bool albumVisible, bool genreVisible, bool lengthVisible, bool albumArtistVisible, bool trackNumberVisible, bool yearVisible, bool bitrateVisible)
        {
            List<string> visibleColumns = new List<string>();

            if (ratingVisible)
                visibleColumns.Add("rating");
            if (artistVisible)
                visibleColumns.Add("artist");
            if (albumVisible)
                visibleColumns.Add("album");
            if (genreVisible)
                visibleColumns.Add("genre");
            if (lengthVisible)
                visibleColumns.Add("length");
            if (albumArtistVisible)
                visibleColumns.Add("albumartist");
            if (trackNumberVisible)
                visibleColumns.Add("tracknumber");
            if (yearVisible)
                visibleColumns.Add("year");
            if (bitrateVisible)
                visibleColumns.Add("bitrate");

            XmlSettingsClient.Instance.Set<string>("TracksGrid", "VisibleColumns", string.Join(";", visibleColumns.ToArray()));
        }

    }
}
