﻿using Microsoft.Data.Sqlite;
using Riff.Data.Access;
using System;
using System.Collections.Generic;

namespace Riff.Data.Sqlite
{
    public sealed class GenreTable : IGenreAccessor, ITable
    {
        private readonly SqliteConnection connection;
        private readonly DataExtractor extractor;

        public GenreTable(SqliteConnection connection, DataExtractor extractor)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.extractor = extractor;
        }

        public string Name { get; } = "Genre";

        public void HandleUpgrade(Version version)
        {
            if (version == Version.Initial)
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"CREATE TABLE {Name} (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name VARCHAR)";
                    command.ExecuteNonQuery();
                }
            }

            if (version == Version.AddIndexes)
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"CREATE INDEX Idx_{Name}_Name ON {Name}(Name)";
                    command.ExecuteNonQuery();
                }
            }
        }

        public Genre Add(Genre genre)
        {
            if (genre.Id.HasValue)
            {
                throw new ArgumentException(nameof(genre.Id));
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"INSERT INTO {Name} (Name) VALUES(@Name)";
                command.Parameters.AddWithValue("@Name", genre.Name);

                command.ExecuteNonQuery();
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "select last_insert_rowid()";
                genre.Id = (long)command.ExecuteScalar();
            }

            return genre;
        }

        public Genre Find(string genreName)
        {
            if (string.IsNullOrEmpty(genreName))
            {
                throw new ArgumentNullException(nameof(genreName));
            }

            Genre genre = null;

            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT Id AS GenreId, Name AS GenreName FROM {Name} WHERE Name = @Name LIMIT 1";
                command.Parameters.AddWithValue("@Name", genreName);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        genre = extractor.ExtractGenre(reader);
                    }
                }
            }

            return genre;
        }

        public Genre Get(long id)
        {
            Genre genre = null;

            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT Id AS GenreId, Name AS GenreName FROM {Name} WHERE Id = @Id";
                command.Parameters.AddWithValue("@Id", id);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        genre = extractor.ExtractGenre(reader);
                    }
                }
            }

            return genre;
        }

        public IList<Genre> GetAll()
        {
            List<Genre> genres = new List<Genre>();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT Id AS GenreId, Name AS GenreName FROM {Name}";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        genres.Add(extractor.ExtractGenre(reader));
                    }
                }
            }

            return genres;
        }

        public Genre Update(Genre genre)
        {
            if (!genre.Id.HasValue)
            {
                throw new ArgumentNullException(nameof(genre.Id));
            }

            if (string.IsNullOrEmpty(genre.Name))
            {
                throw new ArgumentNullException(nameof(genre.Name));
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"UPDATE {Name} SET Name = @Name WHERE Id = @Id";
                command.Parameters.AddWithValue("@Name", genre.Name);
                command.Parameters.AddWithValue("@Id", genre.Id.Value);

                command.ExecuteNonQuery();
            }

            return genre;
        }
    }
}