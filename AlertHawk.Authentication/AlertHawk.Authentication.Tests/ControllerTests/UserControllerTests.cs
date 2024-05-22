
using System.Security.Claims;
using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Controllers;
using AlertHawk.Authentication.Domain.Custom;
using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AlertHawk.Authentication.Tests.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IGetOrCreateUserService> _mockGetOrCreateUserService;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockGetOrCreateUserService = new Mock<IGetOrCreateUserService>();

            _controller = new UserController(_mockUserService.Object, _mockGetOrCreateUserService.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task PostUserCreation_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("error", "some error");
            var user = new  UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: true) ;
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);
            // Act
            var result = await _controller.PostUserCreation(new UserCreation
            {
                Username = null,
                Password = null,
                RepeatPassword = null,
                UserEmail = null
            });

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PostUserCreation_PasswordsDoNotMatch_ReturnsBadRequest()
        {
            // Arrange
            var userCreation = new UserCreation
            {
                Password = "password1",
                RepeatPassword = "password2",
                Username = null,
                UserEmail = null
            };
            var user = new  UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: true) ;
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);
            // Act
            var result = await _controller.PostUserCreation(userCreation);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var message = Assert.IsType<Message>(badRequestResult.Value);

            Assert.Equal("Passwords do not match.", message.Content);
        }

        [Fact]
        public async Task PostUserCreation_UserAlreadyExists_ReturnsBadRequest()
        {
            // Arrange
            var userCreation = new UserCreation
            {
                Password = "password",
                RepeatPassword = "password",
                Username = null,
                UserEmail = null
            };
            _mockUserService.Setup(s => s.Create(userCreation)).ThrowsAsync(new InvalidOperationException("User already exists"));
            var user = new  UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: true) ;
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);
            // Act
            var result = await _controller.PostUserCreation(userCreation);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var message = Assert.IsType<Message>(badRequestResult.Value);

            Assert.Equal("User already exists", message.Content);
        }

        [Fact]
        public async Task PostUserCreation_ValidUserCreation_ReturnsOk()
        {
            // Arrange
            var userCreation = new UserCreation
            {
                Password = "password",
                RepeatPassword = "password",
                Username = null,
                UserEmail = null
            };
            var user = new  UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: true) ;
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);
            // Act
            var result = await _controller.PostUserCreation(userCreation);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var message = Assert.IsType<Message>(okResult.Value);

            Assert.Equal("User account created successfully.", message.Content);
        }

        [Fact]
        public async Task PutUserUpdate_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("error", "some error");
            var user = new  UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: true) ;
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);
            // Act
            var result = await _controller.PutUserUpdate(new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: false));

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
        [Fact]
        public async Task PutUserUpdate_ThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var user = new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: false);
            _mockUserService.Setup(s => s.Update(user)).ThrowsAsync(new Exception("Unexpected error"));
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.PutUserUpdate(user);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);

            var message = Assert.IsType<Message>(objectResult.Value);
            Assert.Equal("Something went wrong.", message.Content);
        }

        [Fact]
        public async Task PutUserUpdate_ValidUserUpdate_ReturnsOk()
        {
            // Arrange
            var userUpdate = new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: false);
            var user = new  UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: true) ;
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);
            // Act
            var result = await _controller.PutUserUpdate(userUpdate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var message = Assert.IsType<Message>(okResult.Value);
            Assert.Equal("User account updated successfully.", message.Content);
        }

        [Fact]
        public async Task PutUserUpdate_UserNotFound_ReturnsBadRequest()
        {
            // Arrange
            var userUpdate = new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: false);
            _mockUserService.Setup(s => s.Update(userUpdate)).ThrowsAsync(new InvalidOperationException("User not found"));
            var user = new  UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: true) ;
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);
            // Act
            var result = await _controller.PutUserUpdate(userUpdate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var message = Assert.IsType<Message>(badRequestResult.Value);
            Assert.Equal("User not found", message.Content);
        }

        [Fact]
        public async Task PostUserCreation_ThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var userCreation = new UserCreation
            {
                Password = "password",
                RepeatPassword = "password",
                Username = null,
                UserEmail = null
            };

            _mockUserService.Setup(s => s.Create(userCreation)).ThrowsAsync(new Exception("Unexpected error"));

            var user = new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: false);

            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.PostUserCreation(userCreation);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);

            var message = Assert.IsType<Message>(objectResult.Value);
            Assert.Equal("Something went wrong.", message.Content);
        }
        
        [Fact]
        public async Task ResetPassword_ValidUsername_ReturnsOk()
        {
            // Arrange
            var username = "testuser";

            // Act
            var result = await _controller.ResetPassword(username);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task GetAll_ValidRequest_ReturnsOkWithUsers()
        {
            // Arrange
            var users = new List<UserDto> { new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: true) };
            _mockUserService.Setup(s => s.GetAll()).ReturnsAsync(users);
            var user = new  UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: true) ;
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);
            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUsers = Assert.IsType<List<UserDto>>(okResult.Value);
            Assert.Equal(users, returnedUsers);
        }

        [Fact]
        public async Task GetById_ValidUserId_ReturnsOkWithUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: false);
            _mockUserService.Setup(s => s.Get(userId)).ReturnsAsync(user);

            // Act
            var result = await _controller.GetById(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUser = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(user, returnedUser);
        }

        [Fact]
        public async Task GetByEmail_ValidUserEmail_ReturnsOkWithUser()
        {
            // Arrange
            var userEmail = "user@example.com";
            var user = new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: false);
            _mockUserService.Setup(s => s.GetByEmail(userEmail)).ReturnsAsync(user);

            // Act
            var result = await _controller.GetByEmail(userEmail);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUser = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(user, returnedUser);
        }

        [Fact]
        public async Task GetByUsername_ValidUserName_ReturnsOkWithUser()
        {
            // Arrange
            var userName = "user1";
            var user = new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: false);
            _mockUserService.Setup(s => s.GetByUsername(userName)).ReturnsAsync(user);

            // Act
            var result = await _controller.GetByUsername(userName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUser = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(user, returnedUser);
        }
        [Fact]
        public async Task GetByEmail_ValidEmail_ReturnsOkWithUser()
        {
            // Arrange
            var userEmail = "user@example.com";
            var user = new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: false);
            _mockUserService.Setup(s => s.GetByEmail(userEmail)).ReturnsAsync(user);

            // Act
            var result = await _controller.Get(userEmail);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUser = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(user, returnedUser);
        }
        [Fact]
        public async Task GetByEmail_ValidEmail_ReturnsOkWithUserFromToken()
        {
            // Arrange
            var userEmail = "user@example.com";
            var user = new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: false);
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            // Act
            var result = await _controller.Get(userEmail);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUser = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(user, returnedUser);
        }
       [ Fact]
        public async Task GetByEmail_ValidEmail_ReturnsOkWithNoUser()
        {
            // Arrange
            var userEmail = "user@example.com";
            // Act
            var result = await _controller.Get(userEmail);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(okResult.Value);
            
        }
    }
}
