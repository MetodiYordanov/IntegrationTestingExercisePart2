using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MoviesLibraryAPI.Controllers;
using MoviesLibraryAPI.Controllers.Contracts;
using MoviesLibraryAPI.Data.Models;
using MoviesLibraryAPI.Services;
using MoviesLibraryAPI.Services.Contracts;
using System.ComponentModel.DataAnnotations;

namespace MoviesLibraryAPI.Tests
{
    [TestFixture]
    public class NUnitIntegrationTests
    {
        private MoviesLibraryNUnitTestDbContext _dbContext;
        private IMoviesLibraryController _controller;
        private IMoviesRepository _repository;
        IConfiguration _configuration;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
        }

        [SetUp]
        public async Task Setup()
        {
            string dbName = $"MoviesLibraryTestDb_{Guid.NewGuid()}";
            _dbContext = new MoviesLibraryNUnitTestDbContext(_configuration, dbName);

            _repository = new MoviesRepository(_dbContext.Movies);
            _controller = new MoviesLibraryController(_repository);
        }

        [TearDown]
        public async Task TearDown()
        {
            await _dbContext.ClearDatabaseAsync();
        }

        [Test]
        public async Task AddMovieAsync_WhenValidMovieProvided_ShouldAddToDatabase()
        {
            // Arrange
            var movie = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 86,
                Rating = 7.5
            };

            // Act
            await _controller.AddAsync(movie);

            // Assert
            var resultMovie = await _dbContext.Movies.Find(m => m.Title == "Test Movie").FirstOrDefaultAsync();
            Assert.IsNotNull(resultMovie);
        }

        [Test]
        public async Task AddMovieAsync_WhenInvalidMovieProvided_ShouldThrowValidationException()
        {
            // Arrange
            var invalidMovie = new Movie
            {
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 86,
                Rating = 7.5
                // Provide an invalid movie object, for example, missing required fields like 'Title'
                // Assuming 'Title' is a required field, do not set it
            };

            // Act and Assert
            // Expect a ValidationException because the movie is missing a required field
            var exception = Assert.ThrowsAsync<ValidationException>(() => _controller.AddAsync(invalidMovie));
        }

        [Test]
        public async Task DeleteAsync_WhenValidTitleProvided_ShouldDeleteMovie()
        {
            // Arrange
            var movie = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 86,
                Rating = 7.5
            };

            await _controller.AddAsync(movie);

            // Act            
            await _controller.DeleteAsync(movie.Title);

            // Assert
            // The movie should no longer exist in the database
            var resultMovie = await _dbContext.Movies.Find(m => m.Title == "Test Movie").FirstOrDefaultAsync();
            Assert.IsNull(resultMovie);
        }


        [Test]
        public async Task DeleteAsync_WhenTitleIsNull_ShouldThrowArgumentException()
        {
            // Act and Assert
            Assert.ThrowsAsync<ArgumentException>(() => _controller.DeleteAsync(null));
        }

        [Test]
        public async Task DeleteAsync_WhenTitleIsEmpty_ShouldThrowArgumentException()
        {
            // Act and Assert
            Assert.ThrowsAsync<ArgumentException>(() => _controller.DeleteAsync(""));
        }

        [Test]
        public async Task DeleteAsync_WhenTitleDoesNotExist_ShouldThrowInvalidOperationException()
        {
            // Act and Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => _controller.DeleteAsync("Non exisitng title"));
        }

        [Test]
        public async Task GetAllAsync_WhenNoMoviesExist_ShouldReturnEmptyList()
        {
            // Act
            var result = await _controller.GetAllAsync();

            // Assert
            Assert.IsEmpty(result);
        }

        [Test]
        public async Task GetAllAsync_WhenMoviesExist_ShouldReturnAllMovies()
        {
            // Arrange
            var firstMovie = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 86,
                Rating = 7.5
            };
            await _controller.AddAsync(firstMovie);

            var secondMovie = new Movie
            {
                Title = "Test Movie Part 2",
                Director = "Test Director",
                YearReleased = 2023,
                Genre = "Action",
                Duration = 95,
                Rating = 8.1
            };
            await _controller.AddAsync(secondMovie);

            // Act
            var allMovies = await _controller.GetAllAsync();

            // Assert
            // Ensure that all movies are returned
            Assert.IsNotEmpty(allMovies);
            Assert.AreEqual(2, allMovies.Count());

            var hasFirstMovie = allMovies.Any(m => m.Title == firstMovie.Title);
            Assert.IsTrue(hasFirstMovie);
        }

        [Test]
        public async Task GetByTitle_WhenTitleExists_ShouldReturnMatchingMovie()
        {
            // Arrange
            var movie = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 86,
                Rating = 7.5
            };
            await _controller.AddAsync(movie);

            // Act
            var result = await _controller.GetByTitle(movie.Title);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(movie.Title, result.Title);
            Assert.AreEqual(movie.Director, result.Director);
            Assert.AreEqual(movie.YearReleased, result.YearReleased);
            Assert.AreEqual(movie.Genre, result.Genre);
            Assert.AreEqual(movie.Duration, result.Duration);
            Assert.AreEqual(movie.Rating, result.Rating);
        }

        [Test]
        public async Task GetByTitle_WhenTitleDoesNotExist_ShouldReturnNull()
        {
            // Act
            var result = await _controller.GetByTitle("Fake Title");

            // Assert
            Assert.IsNull(result);
        }


        [Test]
        public async Task SearchByTitleFragmentAsync_WhenTitleFragmentExists_ShouldReturnMatchingMovies()
        {
            // Arrange
            var firstMovie = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 86,
                Rating = 7.5
            };

            var secondMovie = new Movie
            {
                Title = "Test Movie Part 2",
                Director = "Test Director",
                YearReleased = 2023,
                Genre = "Action",
                Duration = 95,
                Rating = 8.1
            };

            await _dbContext.Movies.InsertManyAsync(new[] { firstMovie, secondMovie});

            // Act
            var result = await _controller.SearchByTitleFragmentAsync("Part");

            // Assert // Should return one matching movie
            Assert.IsNotEmpty(result);
            Assert.AreEqual(1, result.Count());
        }

        [Test]
        public async Task SearchByTitleFragmentAsync_WhenNoMatchingTitleFragment_ShouldThrowKeyNotFoundException()
        {
            // Act and Assert
            Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.SearchByTitleFragmentAsync("Does not exist"));
        }

        [Test]
        public async Task UpdateAsync_WhenValidMovieProvided_ShouldUpdateMovie()
        {
            // Arrange
            var firstMovie = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 86,
                Rating = 7.5
            };

            var secondMovie = new Movie
            {
                Title = "Test Movie Part 2",
                Director = "Test Director",
                YearReleased = 2023,
                Genre = "Action",
                Duration = 95,
                Rating = 8.1
            };

            await _dbContext.Movies.InsertManyAsync(new[] { firstMovie, secondMovie });

            // Modify the movie
            firstMovie.Title = $"{firstMovie.Title} Updated";
            firstMovie.Rating = 10;

            // Act
            await _controller.UpdateAsync(firstMovie);

            // Assert
            var result = await _dbContext.Movies.Find(m => m.Title == firstMovie.Title).FirstOrDefaultAsync();
            Assert.IsNotNull(result);
            Assert.AreEqual(firstMovie.Title, result.Title);
        }

        [Test]
        public async Task UpdateAsync_WhenInvalidMovieProvided_ShouldThrowValidationException()
        {
            // Arrange
            // Movie without required fields
            var movie = new Movie
            {
                Director = "Test Director",
                YearReleased = 2023,
                Genre = "Action",
                Duration = 95,
                Rating = 8.1
            };

            // Act and Assert
            Assert.ThrowsAsync<ValidationException>(() => _controller.UpdateAsync(movie));
        }


        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await _dbContext.ClearDatabaseAsync();
        }
    }
}
