import React from 'react';
import {View, Text, StyleSheet, StatusBar} from 'react-native';
import Constants from '../../styles/Constants';

const Header = props => {
  return (
    <View style={styles.container}>
      <Text style={styles.headerText}>{props.title}</Text>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    backgroundColor: Constants.colors.primary,
    paddingTop: StatusBar.currentHeight,
    paddingBottom: 10,
  },
  headerText: {
    fontSize: 20,
    color: Constants.colors.textColor,
    marginLeft: 16,
  },
});

export default Header;
