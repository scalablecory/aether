import React, {useEffect, useState} from 'react';
import {
  ActivityIndicator,
  FlatList,
  Image,
  PermissionsAndroid,
  Platform,
  SafeAreaView,
  ScrollView,
  StatusBar,
  StyleSheet,
  Text,
  TouchableHighlight,
  View,
  TouchableOpacity,
} from 'react-native';
import Header from './components/Header';
import {BleService} from './service/BleService';
import {BleCallbackInfo, BlePeripheral} from './service/BleService.types';
import Constants from './styles/Constants';

let BluetoothService: BleService;

const App: React.FC = () => {
  const [peripherals, setPeripherals] = useState<BlePeripheral[]>([]);
  const [isScanning, setIsScanning] = useState(false);
  const [isBleServiceReady, setIsBleServiceReady] = useState(false);

  useEffect(() => {
    BluetoothService = new BleService((info: BleCallbackInfo) => {
      setIsScanning(info.isScanning);
      setPeripherals(info.results);
    });

    BluetoothService.initialize();
    checkPermissions();
    setIsBleServiceReady(true);

    return () => {
      BluetoothService.remove();
    };
  }, []);

  const checkPermissions = () => {
    if (Platform.OS === 'android' && Platform.Version >= 23) {
      PermissionsAndroid.check(
        PermissionsAndroid.PERMISSIONS.ACCESS_FINE_LOCATION,
      ).then(result => {
        if (result) {
          console.log('Permission is OK - ', result);
        } else {
          PermissionsAndroid.request(
            PermissionsAndroid.PERMISSIONS.ACCESS_FINE_LOCATION,
          ).then(result => {
            if (result) {
              console.log('User accept - ', result);
            } else {
              console.log('User refuse - ', result);
            }
          });
        }
      });
    }
  };

  const renderItem = (item: BlePeripheral) => {
    const color = item.connected ? 'green' : '#fff';
    return (
      <TouchableHighlight
        style={styles.device}
        onPress={() => BluetoothService.testPeripheral(item)}>
        <View style={[styles.deviceInfo, {backgroundColor: color}]}>
          <Text style={styles.deviceText}>{item.name}</Text>
          <Image
            style={styles.bluetoothImage}
            source={require('./assets/BLE.png')}
          />
        </View>
      </TouchableHighlight>
    );
  };

  if (!isBleServiceReady) {
    return (
      <View style={[styles.activityContainer, styles.activityHorizontal]}>
        <ActivityIndicator size="large" color={Constants.colors.primary} />
      </View>
    );
  }

  return (
    <View style={styles.screen}>
      <StatusBar
        barStyle="dark-content"
        backgroundColor={Constants.colors.primary}
      />
      <SafeAreaView>
        <Header title="Connect Aether device" />
        <ScrollView contentInsetAdjustmentBehavior="automatic">
          <View>
            <View style={{margin: 10}}>
              <TouchableOpacity
                style={styles.buttonScan}
                onPress={() => BluetoothService.startScan(3)}>
                <Text style={styles.buttonScanText}>
                  {'Scan Bluetooth (' + (isScanning ? 'on' : 'off') + ')'}
                </Text>
              </TouchableOpacity>
            </View>

            <View style={{margin: 10}}>
              <TouchableOpacity
                style={styles.buttonScan}
                onPress={() => BluetoothService.retrieveConnected()}>
                <Text style={styles.buttonScanText}>
                  {'Retrieve connected peripherals'}
                </Text>
              </TouchableOpacity>
            </View>

            {peripherals.length == 0 && (
              <View style={{flex: 1, margin: 20}}>
                <Text style={{textAlign: 'center'}}>No peripherals</Text>
              </View>
            )}
          </View>
        </ScrollView>
        <FlatList
          data={peripherals}
          renderItem={({item}) => renderItem(item)}
          keyExtractor={item => item.id}
        />
      </SafeAreaView>
    </View>
  );
};

const styles = StyleSheet.create({
  screen: {
    flex: 1,
    backgroundColor: Constants.colors.screen,
  },
  device: {
    backgroundColor: Constants.colors.white,
    margin: 8,
    padding: 10,
    borderBottomWidth: 1,
    borderBottomColor: Constants.colors.primary,
    flexDirection: 'row',
  },
  deviceInfo: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    width: '100%',
  },
  deviceText: {
    fontSize: 18,
    textTransform: 'capitalize',
  },
  activityContainer: {
    flex: 1,
    justifyContent: 'center',
  },
  activityHorizontal: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    padding: 10,
  },
  bluetoothImage: {
    width: 12,
    height: 20,
  },
  buttonScan: {
    backgroundColor: Constants.colors.primary,
    padding: 8,
    borderRadius: 4,
  },
  buttonScanText: {
    color: Constants.colors.white,
    textTransform: 'uppercase',
    fontSize: 16,
    textAlign: 'center',
  },
});

export default App;
