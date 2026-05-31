package com.ibaapps.HotelOrderSystem.ui.navigation

import androidx.compose.runtime.Composable
import androidx.navigation.NavHostController
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.ibaapps.HotelOrderSystem.ui.auth.AccessDeniedScreen
import com.ibaapps.HotelOrderSystem.ui.auth.LoginScreen
import com.ibaapps.HotelOrderSystem.ui.main.MainScaffold
import com.ibaapps.HotelOrderSystem.ui.orders.OrderDetailsScreen
import com.ibaapps.HotelOrderSystem.ui.splash.SplashScreen

@Composable
fun AppNavHost(
    deepLinkOrderId: Int? = null,
    onDeepLinkConsumed: () -> Unit = {},
    navController: NavHostController = rememberNavController()
) {
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
                },
                deepLinkOrderId = deepLinkOrderId,
                onDeepLinkConsumed = onDeepLinkConsumed
            )
        }

        composable(
            route = Routes.ORDER_DETAILS,
            arguments = listOf(navArgument(Routes.ARG_ORDER_ID) { type = NavType.IntType })
        ) {
            OrderDetailsScreen(onBack = { navController.popBackStack() })
        }
    }
}
