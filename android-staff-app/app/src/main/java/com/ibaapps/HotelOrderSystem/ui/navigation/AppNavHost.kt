package com.ibaapps.HotelOrderSystem.ui.navigation

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.navigation.NavHostController
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.ibaapps.HotelOrderSystem.ui.auth.AccessDeniedScreen
import com.ibaapps.HotelOrderSystem.ui.auth.LoginScreen
import com.ibaapps.HotelOrderSystem.ui.main.MainScaffold
import com.ibaapps.HotelOrderSystem.ui.splash.SplashScreen

@Composable
fun AppNavHost(navController: NavHostController = rememberNavController()) {
    NavHost(navController = navController, startDestination = Routes.SPLASH) {

        composable(Routes.SPLASH) {
            SplashScreen(
                onLoggedIn = {
                    navController.navigate(Routes.MAIN) {
                        popUpTo(Routes.SPLASH) { inclusive = true }
                    }
                },
                onLoggedOut = {
                    navController.navigate(Routes.LOGIN) {
                        popUpTo(Routes.SPLASH) { inclusive = true }
                    }
                }
            )
        }

        composable(Routes.LOGIN) {
            LoginScreen(
                onAuthenticated = {
                    navController.navigate(Routes.MAIN) {
                        popUpTo(Routes.LOGIN) { inclusive = true }
                    }
                },
                onAccessDenied = { navController.navigate(Routes.ACCESS_DENIED) }
            )
        }

        composable(Routes.ACCESS_DENIED) {
            AccessDeniedScreen(
                onBackToLogin = {
                    navController.navigate(Routes.LOGIN) {
                        popUpTo(Routes.ACCESS_DENIED) { inclusive = true }
                    }
                }
            )
        }

        composable(Routes.MAIN) {
            MainScaffold(
                onOpenOrderDetails = { orderId -> navController.navigate(Routes.orderDetails(orderId)) },
                onLoggedOut = {
                    navController.navigate(Routes.LOGIN) {
                        popUpTo(Routes.MAIN) { inclusive = true }
                    }
                }
            )
        }

        composable(
            route = Routes.ORDER_DETAILS,
            arguments = listOf(navArgument(Routes.ARG_ORDER_ID) { type = NavType.IntType })
        ) { backStackEntry ->
            val orderId = backStackEntry.arguments?.getInt(Routes.ARG_ORDER_ID) ?: 0
            // Placeholder; the real details screen is built in Task 11.
            OrderDetailsPlaceholder(orderId)
        }
    }
}

@Composable
private fun OrderDetailsPlaceholder(orderId: Int) {
    Column(
        modifier = Modifier.fillMaxSize(),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text("Order #$orderId", style = MaterialTheme.typography.titleLarge)
        Text("Details screen lands in Task 11", style = MaterialTheme.typography.bodyMedium)
    }
}
