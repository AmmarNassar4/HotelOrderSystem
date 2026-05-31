package com.ibaapps.HotelOrderSystem.di

import com.ibaapps.HotelOrderSystem.monitor.AndroidNetworkMonitor
import com.ibaapps.HotelOrderSystem.monitor.NetworkMonitor
import dagger.Binds
import dagger.Module
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
abstract class AppModule {

    @Binds
    @Singleton
    abstract fun bindNetworkMonitor(impl: AndroidNetworkMonitor): NetworkMonitor
}
