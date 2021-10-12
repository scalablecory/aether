import 'react-native-gesture-handler';
import * as React from 'react';
import {createDrawerNavigator} from '@react-navigation/drawer';
import {NavigationContainer} from '@react-navigation/native';
import HomeScreen from './pages/Home';
import DevicesScreen from './pages/Devices';

const Drawer = createDrawerNavigator();

const AetherNavigator = () => (
  <NavigationContainer>
    <Drawer.Navigator>
      <Drawer.Screen name="Aether" component={HomeScreen} />
      <Drawer.Screen name="Devices" component={DevicesScreen} />
    </Drawer.Navigator>
  </NavigationContainer>
);

export default AetherNavigator;
