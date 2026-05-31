package com.ibaapps.HotelOrderSystem

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.ibaapps.HotelOrderSystem.ui.auth.AccessDeniedScreen
import com.ibaapps.HotelOrderSystem.ui.auth.LoginScreen
import com.ibaapps.HotelOrderSystem.ui.theme.HotelStaffTheme
import dagger.hilt.android.AndroidEntryPoint

@AndroidEntryPoint
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            HotelStaffTheme {
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = MaterialTheme.colorScheme.background
                ) {
                    StaffAppRoot()
                }
            }
        }
    }
}

/**
 * Root navigation host. Currently: login → home placeholder, plus the
 * access-denied screen. The splash gate and bottom-nav scaffold are added in
 * Task 6.
 */
@Composable
private fun StaffAppRoot() {
    val navController = rememberNavController()
    NavHost(navController = navController, startDestination = "login") {
        composable("login") {
            LoginScreen(
                onAuthenticated = {
                    navController.navigate("home") {
                        popUpTo("login") { inclusive = true }
                    }
                },
                onAccessDenied = { navController.navigate("accessDenied") }
            )
        }
        composable("accessDenied") {
            AccessDeniedScreen(
                onBackToLogin = {
                    navController.navigate("login") {
                        popUpTo("accessDenied") { inclusive = true }
                    }
                }
            )
        }
        composable("home") { HomePlaceholderScreen() }
    }
}

@Composable
private fun HomePlaceholderScreen() {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(text = "Signed in", style = MaterialTheme.typography.headlineSmall)
        Text(
            text = "Main scaffold lands in Task 6",
            style = MaterialTheme.typography.bodyMedium
        )
    }
}
