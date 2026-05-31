package com.ibaapps.HotelOrderSystem.ui.main

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Assignment
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.ToggleOn
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.navigation.NavDestination.Companion.hierarchy
import androidx.navigation.NavGraph.Companion.findStartDestination
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController

private enum class BottomTab(val route: String, val label: String, val icon: ImageVector) {
    Pending("tab/pending", "Pending", Icons.Filled.Notifications),
    MyTasks("tab/my-tasks", "My Tasks", Icons.AutoMirrored.Filled.Assignment),
    Status("tab/status", "Status", Icons.Filled.ToggleOn),
    Profile("tab/profile", "Profile", Icons.Filled.Person)
}

/**
 * Authenticated home: a bottom-nav scaffold over the four staff tabs.
 * Tab content is placeholder here; real screens are added in Tasks 8–14.
 */
@Composable
fun MainScaffold(
    onOpenOrderDetails: (Int) -> Unit,
    onLoggedOut: () -> Unit,
    mainViewModel: MainViewModel = hiltViewModel()
) {
    // Obtaining the view model starts the heartbeat loop for the authenticated session.
    val tabNav = rememberNavController()
    val tabs = BottomTab.entries

    Scaffold(
        bottomBar = {
            NavigationBar {
                val backStackEntry by tabNav.currentBackStackEntryAsState()
                val currentDestination = backStackEntry?.destination
                tabs.forEach { tab ->
                    val selected = currentDestination?.hierarchy?.any { it.route == tab.route } == true
                    NavigationBarItem(
                        selected = selected,
                        onClick = {
                            tabNav.navigate(tab.route) {
                                popUpTo(tabNav.graph.findStartDestination().id) { saveState = true }
                                launchSingleTop = true
                                restoreState = true
                            }
                        },
                        icon = { Icon(tab.icon, contentDescription = tab.label) },
                        label = { Text(tab.label) }
                    )
                }
            }
        }
    ) { innerPadding ->
        NavHost(
            navController = tabNav,
            startDestination = BottomTab.Pending.route,
            modifier = Modifier.padding(innerPadding)
        ) {
            composable(BottomTab.Pending.route) { TabPlaceholder("Pending orders") }
            composable(BottomTab.MyTasks.route) { TabPlaceholder("My active tasks") }
            composable(BottomTab.Status.route) { TabPlaceholder("Status & availability") }
            composable(BottomTab.Profile.route) { TabPlaceholder("Profile") }
        }
    }
}

@Composable
private fun TabPlaceholder(title: String) {
    Column(
        modifier = Modifier.fillMaxSize(),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(text = title, style = MaterialTheme.typography.titleMedium)
    }
}
