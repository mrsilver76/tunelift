/*
 * TuneLift - Export iTunes audio playlists as standard or extended .m3u files.
 * Copyright (C) 2020 Richard Lawrence
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, see
 * <https://www.gnu.org/licenses/>.
 */

namespace TuneLift
{
    /// <summary>
    /// Provides helper methods for correct grammar in messages.
    /// </summary>
    internal sealed class GrammarHelper
    {
        /// <summary>
        /// Pluralises a string based on the number provided.
        /// </summary>
        /// <param name="number">The count to describe.</param>
        /// <param name="singular">The noun to use when the count is 1.</param>
        /// <param name="plural">The noun to use for any other count.</param>
        /// <returns>The count followed by the right noun, for example "1 track" or "2 tracks".</returns>
        public static string Pluralise(int number, string singular, string plural)
        {
            if (number == 1)
                return $"{number} {singular}";
            else
                return $"{number:N0} {plural}";
        }
    }
}
