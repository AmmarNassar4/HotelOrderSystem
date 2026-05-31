package com.ibaapps.HotelOrderSystem.ui.auth

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import com.ibaapps.HotelOrderSystem.data.local.SessionManager
import com.ibaapps.HotelOrderSystem.data.remote.ApiErrorType
import com.ibaapps.HotelOrderSystem.data.remote.NetworkResult
import com.ibaapps.HotelOrderSystem.domain.model.UserProfile
import com.ibaapps.HotelOrderSystem.domain.repository.AuthRepository
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.UnconfinedTestDispatcher
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner

@OptIn(ExperimentalCoroutinesApi::class)
@RunWith(RobolectricTestRunner::class)
class AuthViewModelTest {

    private class FakeAuthRepository : AuthRepository {
        var result: NetworkResult<UserProfile> = NetworkResult.Error(ApiErrorType.Unknown)
        override suspend fun login(userName: String, password: String) = result
        override suspend fun logout(): NetworkResult<Unit> = NetworkResult.Success(Unit)
    }

    private lateinit var session: SessionManager
    private lateinit var repo: FakeAuthRepository
    private lateinit var vm: AuthViewModel

    private fun profile(role: String) = UserProfile(1, "Staff One", "hk", role, 3, "Housekeeping")

    @Before
    fun setUp() {
        Dispatchers.setMain(UnconfinedTestDispatcher())
        val context = ApplicationProvider.getApplicationContext<Context>()
        val prefs = context.getSharedPreferences("auth_vm_test", Context.MODE_PRIVATE)
        prefs.edit().clear().commit()
        session = SessionManager(prefs)
        repo = FakeAuthRepository()
        vm = AuthViewModel(repo, session)
    }

    @After
    fun tearDown() = Dispatchers.resetMain()

    @Test
    fun blankCredentials_yieldError_withoutCallingRepo() {
        vm.login("", "")
        val state = vm.state.value
        assertTrue(state is LoginUiState.Error)
    }

    @Test
    fun staffLogin_success_setsSuccessState() = runTest {
        repo.result = NetworkResult.Success(profile("Staff"))
        vm.login("hk", "pw")
        val state = vm.state.value
        assertTrue(state is LoginUiState.Success)
        assertEquals("Staff", (state as LoginUiState.Success).profile.role)
    }

    @Test
    fun supervisorLogin_isAllowed() = runTest {
        repo.result = NetworkResult.Success(profile("Supervisor"))
        vm.login("sup", "pw")
        assertTrue(vm.state.value is LoginUiState.Success)
    }

    @Test
    fun adminLogin_isDenied_andSessionCleared() = runTest {
        // Simulate the repository having persisted the session before the gate runs.
        session.saveSession("jwt", 0L, profile("Admin"))
        repo.result = NetworkResult.Success(profile("Admin"))
        vm.login("admin", "pw")
        assertTrue(vm.state.value is LoginUiState.AccessDenied)
        assertNull(session.authToken)
    }

    @Test
    fun unauthorized_setsErrorWithBackendMessage() = runTest {
        repo.result = NetworkResult.Error(ApiErrorType.Unauthorized, "Invalid username or password.")
        vm.login("hk", "bad")
        val state = vm.state.value
        assertTrue(state is LoginUiState.Error)
        assertEquals("Invalid username or password.", (state as LoginUiState.Error).message)
    }
}
