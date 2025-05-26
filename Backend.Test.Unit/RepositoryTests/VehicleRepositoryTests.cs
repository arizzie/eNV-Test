// Backend.Tests.Unit/RepositoryTests/VehicleRepositoryTests.cs
using Backend.Test.Unit.TestHelpers;
using Data; // Your EvnContext
using Data.Models; // Your Vehicle, GetVinsQuery, PaginationResponse, AdditionalVehicleInfo, Variable, VehicleVariable
using Data.Repository; // Your VehicleRepository
using EFCore.BulkExtensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System; // For DateTime
using System.Collections.Generic;
using System.Linq;
using System.Threading; // For CancellationToken
using System.Threading.Tasks;
using Xunit;

namespace Backend.Tests.Unit.RepositoryTests
{
    public class VehicleRepositoryTests
    {
        private readonly Mock<EvnContext> _mockContext;
        private readonly Mock<ILogger<VehicleRepository>> _mockLogger;
        private readonly VehicleRepository _repository;
        private List<Vehicle> _seedVehicles; // In-memory data for vehicles
        private List<VehicleVariable> _seedVehicleVariables; // In-memory data for vehicle variables

        public VehicleRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<EvnContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _mockContext = new Mock<EvnContext>(options); 

            // Initialize _mockLogger:
            _mockLogger = new Mock<ILogger<VehicleRepository>>(); 

            _repository = new VehicleRepository(_mockContext.Object, _mockLogger.Object);


            // Initialize seed data for each test run
            _seedVehicles = new List<Vehicle>
            {
                new Vehicle { Vin = "VIN001", DealerId = "DEALER1", ModifiedDate = new DateTime(2023, 1, 15),
                    AdditionalVehicleInfo = new List<AdditionalVehicleInfo>
                    {
                    new AdditionalVehicleInfo { VehicleId = "VIN001", VariableId = 2, Value = "ValueB", Variable = new VehicleVariable { Id = 2, Name = "VariableB" } }, // Assuming Variable.Id is string, but value from API is int
                    new AdditionalVehicleInfo { VehicleId= "VIN001", VariableId = 1, Value = "ValueA", Variable = new VehicleVariable { Id = 1, Name = "VariableA" } }
                    }
                },
                new Vehicle { Vin = "VIN002", DealerId = "DEALER2", ModifiedDate = new DateTime(2023, 2, 10),
                    AdditionalVehicleInfo = new List<AdditionalVehicleInfo>
                    {
                        // CHANGE THIS TO INTEGER:
                        new AdditionalVehicleInfo { VehicleId = "VIN002", VariableId = 3, Value = "ValueC", Variable = new VehicleVariable { Id = 3, Name = "VariableC" } }
                    }
                },
                new Vehicle { Vin = "VIN003", DealerId = "DEALER1", ModifiedDate = new DateTime(2023, 3, 5) },
                new Vehicle { Vin = "VIN004", DealerId = "DEALER3", ModifiedDate = new DateTime(2023, 4, 20) },
                new Vehicle { Vin = "VIN005", DealerId = "DEALER2", ModifiedDate = new DateTime(2023, 1, 20) }
            };

            _seedVehicleVariables = new List<VehicleVariable>
            {
                new VehicleVariable { Id = 1, Name = "Variable 1 Name" },
                new VehicleVariable { Id = 2, Name = "Variable 2 Name" },
                new VehicleVariable { Id = 3, Name = "Variable 3 Name" }
            };


            _mockContext.Setup(c => c.Vehicles).Returns(DbSetMocking.CreateMockDbSet(_seedVehicles).Object);
            _mockContext.Setup(c => c.VehicleVariables).Returns(DbSetMocking.CreateMockDbSet(_seedVehicleVariables).Object);

            _mockContext.Setup(m => m.BulkInsertOrUpdateAsync(
                It.IsAny<IEnumerable<Vehicle>>(),
                It.IsAny<BulkConfig>(),
                It.IsAny<Action<decimal>>(),
                null,
                It.IsAny<CancellationToken>()
            )).Returns(Task.CompletedTask)
              .Callback<IEnumerable<Vehicle>, Action<BulkConfig>, Action<decimal>, CancellationToken>((vehicles, config, progress, ct) =>
              {
                  // Simulate the effect of the bulk operation on the in-memory data
                  foreach (var vehicle in vehicles)
                  {
                      var existing = _seedVehicles.FirstOrDefault(v => v.Vin == vehicle.Vin);
                      if (existing == null)
                      {
                          _seedVehicles.Add(vehicle); // Simulate insert
                      }
                      else
                      {
                          existing.DealerId = vehicle.DealerId;
                          existing.ModifiedDate = vehicle.ModifiedDate;
                      }
                  }
              });
        }

        #region GetVehicleByVinAsync Tests (retained from previous example, no changes needed)

        [Fact]
        public async Task GetVehicleByVinAsync_ShouldReturnVehicle_WhenVinExists()
        {
            string vinToFind = "VIN001";
            var result = await _repository.GetVehicleByVinAsync(vinToFind);

            result.Should().NotBeNull();
            result.Vin.Should().Be(vinToFind);
        }

        [Fact]
        public async Task GetVehicleByVinAsync_ShouldReturnNull_WhenVinDoesNotExist()
        {
            string vinToFind = "NONEXISTENT";
            var result = await _repository.GetVehicleByVinAsync(vinToFind);

            result.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task GetVehicleByVinAsync_ShouldReturnNullAndLogWarning_WhenVinIsNullOrWhiteSpace(string vin)
        {
            var result = await _repository.GetVehicleByVinAsync(vin);

            result.Should().BeNull();
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Warning),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Attempted to get vehicle data with empty or null VIN.")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task GetVehicleByVinAsync_ShouldReturnVehicleWithSortedAdditionalInfo()
        {
            string vinToFind = "VIN001";
            var result = await _repository.GetVehicleByVinAsync(vinToFind);

            result.Should().NotBeNull();
            result.AdditionalVehicleInfo.Should().NotBeEmpty();
            result.AdditionalVehicleInfo.Select(avi => avi.VariableId)
                  .Should().ContainInOrder(1, 2);
        }

        #endregion

        #region GetVehiclesAsync Tests (retained from previous example, no changes needed)

        [Fact]
        public async Task GetVehiclesAsync_ShouldReturnAllVehicles_WhenNoFiltersOrPagination()
        {
            var query = new GetVinsQuery();
            var response = await _repository.GetVehiclesAsync(query);

            response.Should().NotBeNull();
            response.TotalCount.Should().Be(_seedVehicles.Count);
            response.Items.Should().HaveCount(_seedVehicles.Count);
            response.Items.Select(v => v.DealerId).Should().BeInAscendingOrder();
        }

        [Fact]
        public async Task GetVehiclesAsync_ShouldFilterByDealerId()
        {
            var query = new GetVinsQuery { DealerId = "DEALER1" };
            var expectedVehicles = _seedVehicles.Where(v => v.DealerId.Contains("DEALER1")).ToList();
            var response = await _repository.GetVehiclesAsync(query);

            response.TotalCount.Should().Be(expectedVehicles.Count);
            response.Items.Should().HaveCount(expectedVehicles.Count);
            response.Items.Should().OnlyContain(v => v.DealerId.Contains("DEALER1"));
        }

        [Fact]
        public async Task GetVehiclesAsync_ShouldFilterByModifiedDate()
        {
            var query = new GetVinsQuery { ModifiedDate = "02/01/2023" };
            var expectedDate = new DateTime(2023, 2, 1);
            var expectedVehicles = _seedVehicles.Where(v => v.ModifiedDate.Date > expectedDate.Date).ToList();
            var response = await _repository.GetVehiclesAsync(query);

            response.TotalCount.Should().Be(expectedVehicles.Count);
            response.Items.Should().HaveCount(expectedVehicles.Count);
            response.Items.Should().OnlyContain(v => v.ModifiedDate.Date > expectedDate.Date);
        }

        [Fact]
        public async Task GetVehiclesAsync_ShouldLogWarningAndSkipFilter_WhenInvalidModifiedDate()
        {
            var query = new GetVinsQuery { ModifiedDate = "invalid-date-format" };
            var response = await _repository.GetVehiclesAsync(query);

            response.TotalCount.Should().Be(_seedVehicles.Count);
            response.Items.Should().HaveCount(_seedVehicles.Count);
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Warning),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Invalid 'modifiedDate' format received:")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Theory]
        [InlineData("Vin", "ascending", "VIN001", "VIN002", "VIN003", "VIN004", "VIN005")]
        [InlineData("Vin", "descending", "VIN005", "VIN004", "VIN003", "VIN002", "VIN001")]
        [InlineData("ModifiedDate", "desc", "VIN004", "VIN003", "VIN002", "VIN005", "VIN001")]
        [InlineData("DealerId", "asc", "VIN001", "VIN003", "VIN002", "VIN005", "VIN004")]
        public async Task GetVehiclesAsync_ShouldApplySortingCorrectly(
            string sortBy, string sortDirection, params string[] expectedVinOrder)
        {
            var query = new GetVinsQuery { SortBy = sortBy, SortDirection = sortDirection };
            var response = await _repository.GetVehiclesAsync(query);

            response.Items.Should().NotBeEmpty();
            response.Items.Select(v => v.Vin).Should().ContainInOrder(expectedVinOrder);
        }

        [Fact]
        public async Task GetVehiclesAsync_ShouldDefaultSort_WhenInvalidSortColumn()
        {
            var query = new GetVinsQuery { SortBy = "InvalidColumn", SortDirection = "ascending" };
            var response = await _repository.GetVehiclesAsync(query);

            response.Items.Should().NotBeEmpty();
            response.Items.Select(v => v.DealerId).Should().BeInAscendingOrder();
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Warning),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Invalid sort column 'InvalidColumn' provided.")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Theory]
        [InlineData(1, 2, "VIN001", "VIN003")]
        [InlineData(2, 2, "VIN002", "VIN005")]
        [InlineData(3, 2, "VIN004")]
        [InlineData(1, 5, "VIN001", "VIN003", "VIN002", "VIN005", "VIN004")]
        public async Task GetVehiclesAsync_ShouldApplyPaginationCorrectly(
            int pageNumber, int pageSize, params string[] expectedVinOrder)
        {
            var query = new GetVinsQuery { PageNumber = pageNumber, PageSize = pageSize, SortBy = "DealerId", SortDirection = "asc" };
            var response = await _repository.GetVehiclesAsync(query);

            response.Items.Should().HaveCount(expectedVinOrder.Length);
            response.Items.Select(v => v.Vin).Should().ContainInOrder(expectedVinOrder);
            response.TotalCount.Should().Be(_seedVehicles.Count);
        }

        [Theory]
        [InlineData(null, 10)]
        [InlineData(1, null)]
        [InlineData(0, 10)]
        [InlineData(1, 0)]
        public async Task GetVehiclesAsync_ShouldSkipPaginationAndLogWarning_WhenInvalidPagination(int? pageNumber, int? pageSize)
        {
            var query = new GetVinsQuery { PageNumber = pageNumber, PageSize = pageSize };
            var response = await _repository.GetVehiclesAsync(query);

            response.TotalCount.Should().Be(_seedVehicles.Count);
            response.Items.Should().HaveCount(_seedVehicles.Count);
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Warning),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Invalid pagination parameters:")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task GetVehiclesAsync_ShouldCombineFiltersAndPagination()
        {
            var query = new GetVinsQuery
            {
                DealerId = "DEALER1",
                PageNumber = 1,
                PageSize = 1,
                SortBy = "Vin",
                SortDirection = "asc"
            };

            var expectedTotalCount = _seedVehicles.Count(v => v.DealerId == "DEALER1");
            var response = await _repository.GetVehiclesAsync(query);

            response.TotalCount.Should().Be(expectedTotalCount);
            response.Items.Should().HaveCount(1);
            response.Items.First().Vin.Should().Be("VIN001");
        }

        #endregion

        #region SaveVehiclesBatchAsync Tests

        [Fact]
        public async Task SaveVehiclesBatchAsync_ShouldCallBulkInsertOrUpdateAsync()
        {
            // Arrange
            var vehiclesToSave = new List<Vehicle>
            {
                new Vehicle { Vin = "NEWVIN01", DealerId = "NEWDLR", ModifiedDate = DateTime.UtcNow },
                new Vehicle { Vin = "VIN001", DealerId = "UPDATEDDLR", ModifiedDate = DateTime.UtcNow.AddHours(1) } // Existing VIN
            };

            // Act
            var count = await _repository.SaveVehiclesBatchAsync(vehiclesToSave);

            // Assert
            _mockContext.Verify(m => m.BulkInsertOrUpdateAsync(
                It.Is<IEnumerable<Vehicle>>(list => list.Count() == vehiclesToSave.Count), // Verify correct list passed
                It.IsAny<BulkConfig>(),
                It.IsAny<Action<decimal>>(),
                null,
                It.IsAny<CancellationToken>()
            ), Times.Once);

            count.Should().Be(vehiclesToSave.Count); // Verify the returned count

            // Optionally, verify the state of the in-memory data (mocked effect)
            _seedVehicles.Should().ContainSingle(v => v.Vin == "NEWVIN01");
            _seedVehicles.Should().ContainSingle(v => v.Vin == "VIN001" && v.DealerId == "UPDATEDDLR");
        }

        [Fact]
        public async Task SaveVehiclesBatchAsync_ShouldReturnZero_WhenEmptyBatch()
        {
            // Arrange
            var emptyList = new List<Vehicle>();

            // Act
            var count = await _repository.SaveVehiclesBatchAsync(emptyList);

            // Assert
            _mockContext.Verify(m => m.BulkInsertOrUpdateAsync(
                It.IsAny<IEnumerable<Vehicle>>(),
                It.IsAny<BulkConfig>(),
                It.IsAny<Action<decimal>>(),
                null,
                It.IsAny<CancellationToken>()
            ), Times.Once); // Still calls, but with empty list

            count.Should().Be(0);
        }

        #endregion

        #region GetVariableFilter Tests

        [Fact]
        public async Task GetVariableFilter_ShouldReturnAllVehicleVariables()
        {
            // Arrange
            // _mockContext.Setup(c => c.VehicleVariables) is already set up in constructor

            // Act
            var result = await _repository.GetVariableFilter();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(_seedVehicleVariables.Count);
            result.Select(v => v.Id).Should().Contain(_seedVehicleVariables.Select(sv => sv.Id));
        }

        [Fact]
        public async Task GetVariableFilter_ShouldReturnEmptyList_WhenNoVehicleVariablesExist()
        {
            // Arrange
            _mockContext.Setup(c => c.VehicleVariables).Returns(DbSetMocking.CreateMockDbSet(new List<VehicleVariable>()).Object);

            // Act
            var result = await _repository.GetVariableFilter();

            // Assert
            result.Should().NotBeNull().And.BeEmpty();
        }

        #endregion


        #region GetVehiclesByVinAsync Tests

        [Fact]
        public async Task GetVehiclesByVinAsync_ShouldReturnMatchingVehicles()
        {
            // Arrange
            var vinsToFind = new List<string> { "VIN001", "VIN004" };

            // Act
            var result = await _repository.GetVehiclesByVinAsync(vinsToFind);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Keys.Should().Contain(new[] { "VIN001", "VIN004" });
            result["VIN001"].DealerId.Should().Be("DEALER1");
            result["VIN004"].DealerId.Should().Be("DEALER3");
        }

        [Fact]
        public async Task GetVehiclesByVinAsync_ShouldReturnEmptyDictionary_WhenNoMatches()
        {
            // Arrange
            var vinsToFind = new List<string> { "NONEXISTENT1", "NONEXISTENT2" };

            // Act
            var result = await _repository.GetVehiclesByVinAsync(vinsToFind);

            // Assert
            result.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public async Task GetVehiclesByVinAsync_ShouldHandleEmptyInputList()
        {
            // Arrange
            var emptyVins = new List<string>();

            // Act
            var result = await _repository.GetVehiclesByVinAsync(emptyVins);

            // Assert
            result.Should().NotBeNull().And.BeEmpty();
        }

        #endregion
    }
}